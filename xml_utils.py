# -*- coding: utf-8 -*-
"""Utilities for validating and patching Discogs XML dumps."""
from __future__ import annotations

import gzip
import io
import logging
import os
from contextlib import contextmanager
from dataclasses import dataclass
from enum import Enum
from tempfile import SpooledTemporaryFile
from typing import Iterator, List, Optional, Union

logger = logging.getLogger(__name__)

_STRICT_ENV_VAR = "DISCOGS_XML_STRICT_ROOT"
_START_BUFFER_LIMIT = 64 * 1024
_END_BUFFER_LIMIT = 64 * 1024
_READ_CHUNK_SIZE = 64 * 1024
_SPOOLED_MAX_SIZE = 64 * 1024 * 1024


class RootValidationError(RuntimeError):
    """Raised when strict validation is enabled and an XML dump is malformed."""


class DumpEntity(Enum):
    """Supported dataset types and their expected root tags."""

    ARTIST = ("artist", "artists")
    LABEL = ("label", "labels")
    MASTER = ("master", "masters")
    RELEASE = ("release", "releases")

    def __init__(self, singular: str, root_tag: str) -> None:
        self.singular = singular
        self.root_tag = root_tag

    @classmethod
    def from_key(cls, key: str) -> "DumpEntity":
        key_lower = key.lower()
        for member in cls:
            if key_lower in (member.singular, member.root_tag):
                return member
        raise ValueError("Unknown dump entity: %s" % key)


@dataclass
class RootWrapResult:
    stream: io.BufferedReader
    missing_open: bool
    missing_close: bool
    expected_tag: str
    source_name: str

    @property
    def patched(self) -> bool:
        return self.missing_open or self.missing_close


class _ChainedStream(io.RawIOBase):
    """Lightweight stream that chains multiple binary file-like objects."""

    def __init__(self, parts: List[io.IOBase]) -> None:
        super().__init__()
        self._parts = parts
        self._index = 0

    def readable(self) -> bool:  # pragma: no cover - trivial
        return True

    def read(self, size: int = -1) -> bytes:  # pragma: no cover - simple delegator
        if size == 0:
            return b""
        if size < 0:
            data = b"".join(part.read() for part in self._parts[self._index :])
            self._index = len(self._parts)
            return data
        chunks: List[bytes] = []
        remaining = size
        while remaining > 0 and self._index < len(self._parts):
            part = self._parts[self._index]
            chunk = part.read(remaining)
            if not chunk:
                self._index += 1
                continue
            chunks.append(chunk)
            remaining -= len(chunk)
        return b"".join(chunks)

    def readinto(self, b):  # type: ignore[override]
        data = self.read(len(b))
        n = len(data)
        b[:n] = data
        return n

    def close(self) -> None:
        try:
            for part in self._parts:
                try:
                    part.close()
                except Exception:  # pragma: no cover - defensive
                    logger.debug("Failed to close stream part", exc_info=True)
        finally:
            self._parts = []
            super().close()


def _strict_mode_enabled() -> bool:
    value = os.getenv(_STRICT_ENV_VAR)
    if value is None:
        return False
    return value.lower() in {"1", "true", "yes", "on"}


def _skip_preamble(data: bytes) -> bytes:
    text = data
    if text.startswith(b"\xef\xbb\xbf"):
        text = text[3:]
    text = text.lstrip()
    changed = True
    while changed and text:
        changed = False
        if text.startswith(b"<?"):
            end = text.find(b"?>")
            if end == -1:
                return text
            text = text[end + 2 :].lstrip()
            changed = True
        if text.startswith(b"<!--"):
            end = text.find(b"-->")
            if end == -1:
                return text
            text = text[end + 3 :].lstrip()
            changed = True
        if text.upper().startswith(b"<!DOCTYPE"):
            end = text.find(b">")
            if end == -1:
                return text
            text = text[end + 1 :].lstrip()
            changed = True
    return text


def _extract_first_tag(data: bytes) -> Optional[str]:
    text = _skip_preamble(data)
    while text:
        idx = text.find(b"<")
        if idx == -1:
            return None
        token = text[idx:]
        if token.startswith(b"<!--"):
            end = token.find(b"-->")
            if end == -1:
                return None
            text = token[end + 3 :]
            continue
        if token.startswith(b"<?"):
            end = token.find(b"?>")
            if end == -1:
                return None
            text = token[end + 2 :]
            continue
        if token.startswith(b"<!"):
            end = token.find(b">")
            if end == -1:
                return None
            text = token[end + 1 :]
            continue
        end = token.find(b">")
        if end == -1:
            return None
        tag = token[1:end].split(b" ", 1)[0].strip()
        if tag.startswith(b"/"):
            tag = tag[1:]
        if tag:
            return tag.decode("utf-8", errors="ignore")
        text = token[end + 1 :]
    return None


def _extract_last_closing_tag(data: bytes) -> Optional[str]:
    text = data.rstrip()
    while text:
        idx = text.rfind(b"<")
        if idx == -1:
            break
        token = text[idx:]
        if token.startswith(b"</"):
            end = token.find(b">")
            if end == -1:
                text = text[:idx]
                continue
            name = token[2:end].split(b" ", 1)[0].strip()
            if name:
                return name.decode("utf-8", errors="ignore")
            text = text[:idx]
            continue
        if token.startswith(b"<!--") or token.startswith(b"<?") or token.startswith(b"<!"):
            text = text[:idx]
            continue
        text = text[:idx]
    return None


@contextmanager
def ensure_root_wrapper(
    source: Union[str, os.PathLike],
    entity: DumpEntity,
    *,
    strict: Optional[bool] = None,
) -> Iterator[RootWrapResult]:
    """Ensure an XML dump has the expected root element.

    Yields a buffered binary stream safe for XML parsers. When ``strict``
    (or the ``DISCOGS_XML_STRICT_ROOT`` env var) is enabled, missing
    roots raise :class:`RootValidationError` instead of being patched.
    """

    source_name = os.fspath(source)
    opener = gzip.open if source_name.endswith(".gz") else open
    strict_mode = _strict_mode_enabled() if strict is None else strict

    with opener(source, "rb") as raw_stream:
        spooled = SpooledTemporaryFile(max_size=_SPOOLED_MAX_SIZE)
        head = bytearray()
        tail = b""

        while True:
            chunk = raw_stream.read(_READ_CHUNK_SIZE)
            if not chunk:
                break
            spooled.write(chunk)
            if len(head) < _START_BUFFER_LIMIT:
                remaining = _START_BUFFER_LIMIT - len(head)
                head.extend(chunk[:remaining])
            tail = (tail + chunk)[- _END_BUFFER_LIMIT :]

        if spooled.tell() == 0:
            spooled.close()
            raise RootValidationError("Empty XML source: %s" % source_name)

        expected_tag = entity.root_tag
        first_tag = _extract_first_tag(bytes(head))
        last_tag = _extract_last_closing_tag(tail)

        missing_open = first_tag != expected_tag
        missing_close = last_tag != expected_tag

        if strict_mode and (missing_open or missing_close):
            spooled.close()
            raise RootValidationError(
                "Missing root <%s> in %s (opening=%s, closing=%s)" % (
                    expected_tag,
                    source_name,
                    not missing_open,
                    not missing_close,
                )
            )

        if missing_open or missing_close:
            pieces = []
            if missing_open:
                pieces.append("opening")
            if missing_close:
                pieces.append("closing")
            logger.info(
                "Patched <%s> root for %s (added %s tag)",
                expected_tag,
                source_name,
                " and ".join(pieces),
            )

        spooled.seek(0)

        parts: List[io.IOBase] = []
        if missing_open:
            parts.append(io.BytesIO(("<%s>\n" % expected_tag).encode("utf-8")))
        parts.append(spooled)
        if missing_close:
            parts.append(io.BytesIO(("</%s>\n" % expected_tag).encode("utf-8")))

        chained = _ChainedStream(parts)
        buffered = io.BufferedReader(chained)

        result = RootWrapResult(
            stream=buffered,
            missing_open=missing_open,
            missing_close=missing_close,
            expected_tag=expected_tag,
            source_name=source_name,
        )

        try:
            yield result
        finally:
            buffered.close()
