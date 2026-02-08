#!/usr/bin/env python3
"""
Generate coherent Discogs XML fixtures by sampling releases and closing references.

Example:
  uv run python tests/fixtures/generate_fixtures.py \
    --input-dir tests/samples \
    --output-dir tests/fixtures \
    --size 25 \
    --complexity highest
"""

from __future__ import annotations

import argparse
import gzip
import heapq
import json
import random
import re
import sys
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple
import time
from xml.sax.saxutils import escape as xml_escape

from lxml import etree


@dataclass
class SourceInfo:
    path: Path
    preamble_lines: List[str]
    root_tag: str
    root_attrib: Dict[str, str]
    nsmap: Dict[Optional[str], str]


@dataclass
class Progress:
    label: str
    every: int
    count: int = 0
    start: float = 0.0

    def __post_init__(self) -> None:
        """Initialize timing and emit a start message."""
        self.start = time.perf_counter()
        print(f"{self.label}: starting", file=sys.stderr)

    def tick(self, n: int = 1) -> None:
        """Increment progress and emit a periodic message."""
        if self.every <= 0:
            return
        self.count += n
        if self.count % self.every == 0:
            elapsed = max(time.perf_counter() - self.start, 0.001)
            rate = self.count / elapsed
            print(
                f"{self.label}: {self.count} items ({rate:,.1f}/s)",
                file=sys.stderr,
            )

    def finish(self) -> None:
        """Emit a completion message with total rate."""
        elapsed = max(time.perf_counter() - self.start, 0.001)
        rate = self.count / elapsed if self.count else 0.0
        print(
            f"{self.label}: done {self.count} items in {elapsed:.1f}s ({rate:,.1f}/s)",
            file=sys.stderr,
        )


def make_progress(label: str, every: int) -> Optional[Progress]:
    """Create a Progress helper or return None when disabled."""
    if every <= 0:
        return None
    return Progress(label, every)


def open_xml(path: Path):
    """Open an XML file, transparently handling .gz inputs."""
    if str(path).endswith(".gz"):
        return gzip.open(path, "rb")
    return path.open("rb")


def extract_preamble(path: Path, max_bytes: int = 65536) -> List[str]:
    """Return XML declaration and DOCTYPE lines from the file header."""
    with open_xml(path) as fp:
        data = fp.read(max_bytes)
    text = data.decode("utf-8", errors="replace")
    parts: List[str] = []
    for match in re.finditer(r"<\?xml[^?]*\?>|<!DOCTYPE[^>]*>", text, re.IGNORECASE):
        parts.append(match.group(0).strip())
    return parts


def get_root_info(path: Path) -> Tuple[str, Dict[str, str], Dict[Optional[str], str]]:
    """Extract root tag, attributes, and namespace map from an XML file."""
    with open_xml(path) as fp:
        context = etree.iterparse(fp, events=("start",), huge_tree=True)
        _, root = next(context)
    return root.tag, dict(root.attrib), dict(root.nsmap)


def qname(tag: str, nsmap: Dict[Optional[str], str]) -> str:
    """Convert a namespace-qualified tag into a prefixed name when possible."""
    if tag.startswith("{"):
        uri, local = tag[1:].split("}", 1)
        prefix = None
        for k, v in nsmap.items():
            if v == uri and k is not None:
                prefix = k
                break
        if prefix:
            return f"{prefix}:{local}"
        return local
    return tag


def build_root_tag(
    tag: str, attrib: Dict[str, str], nsmap: Dict[Optional[str], str]
) -> Tuple[str, str]:
    """Build start/end root tags, preserving attributes and namespaces."""
    qn = qname(tag, nsmap)
    attrs: List[str] = []
    for k, v in attrib.items():
        ak = qname(k, nsmap) if k.startswith("{") else k
        attrs.append(f'{ak}="{xml_escape(v)}"')
    for prefix, uri in nsmap.items():
        if prefix is None:
            attrs.append(f'xmlns="{xml_escape(uri)}"')
        else:
            attrs.append(f'xmlns:{prefix}="{xml_escape(uri)}"')
    attr_text = ""
    if attrs:
        attr_text = " " + " ".join(attrs)
    start = f"<{qn}{attr_text}>"
    end = f"</{qn}>"
    return start, end


def iter_entities(
    path: Path, tag: str, progress: Optional[Progress] = None
) -> Iterable[etree._Element]:
    """Yield entities for a given tag using streaming parse and cleanup.

    Uses lxml's huge_tree mode for very large (trusted) Discogs dumps.
    """
    with open_xml(path) as fp:
        context = etree.iterparse(fp, tag=tag, events=("end",), huge_tree=True)
        for _, element in context:
            if progress is not None:
                progress.tick()
            yield element
            element.clear()
            parent = element.getparent()
            if parent is not None:
                while element.getprevious() is not None:
                    del parent[0]


def xpath_count(element: etree._Element, expr: str) -> int:
    """Return integer count for an XPath expression."""
    return int(element.xpath(f"count({expr})"))


def ln(name: str) -> str:
    """Build a local-name XPath selector for namespace-agnostic matching."""
    return f'*[local-name()="{name}"]'


def release_complexity(element: etree._Element) -> Dict[str, int]:
    """Compute a per-release feature count used for selection ranking."""
    counts = {
        "release_artists": xpath_count(element, f"./{ln('artists')}/{ln('artist')}"),
        "release_extraartists": xpath_count(
            element, f"./{ln('extraartists')}/{ln('artist')}"
        ),
        "track_artists": xpath_count(
            element, f".//{ln('tracklist')}//{ln('artists')}/{ln('artist')}"
        ),
        "track_extraartists": xpath_count(
            element, f".//{ln('tracklist')}//{ln('extraartists')}/{ln('artist')}"
        ),
        "labels": xpath_count(element, f"./{ln('labels')}/{ln('label')}"),
        "formats": xpath_count(element, f"./{ln('formats')}/{ln('format')}"),
        "tracks": xpath_count(element, f".//{ln('tracklist')}//{ln('track')}"),
        "identifiers": xpath_count(
            element, f"./{ln('identifiers')}/{ln('identifier')}"
        ),
        "videos": xpath_count(element, f"./{ln('videos')}/{ln('video')}"),
        "companies": xpath_count(element, f"./{ln('companies')}/{ln('company')}"),
        "images": xpath_count(element, f"./{ln('images')}/{ln('image')}"),
        "genres": xpath_count(element, f"./{ln('genres')}/{ln('genre')}"),
        "styles": xpath_count(element, f"./{ln('styles')}/{ln('style')}"),
    }
    counts["total"] = sum(counts.values())
    return counts


def release_id(element: etree._Element) -> Optional[int]:
    """Return release id from the element attribute, if valid."""
    value = element.get("id")
    if value is None:
        return None
    try:
        return int(value)
    except ValueError:
        return None


def master_id(element: etree._Element) -> Optional[int]:
    """Return master id from the element attribute, if valid."""
    value = element.get("id")
    if value is None:
        return None
    try:
        return int(value)
    except ValueError:
        return None


def child_id_text(element: etree._Element) -> Optional[int]:
    """Return integer id from a child <id> element, if present."""
    node = element.find("id")
    if node is None or node.text is None:
        return None
    try:
        return int(node.text)
    except ValueError:
        return None


def collect_release_refs(element: etree._Element) -> Tuple[set, set, set]:
    """Collect master, artist, and label ids referenced by a release."""
    master_ids: set = set()
    artist_ids: set = set()
    label_ids: set = set()

    for node in element.xpath(f"./{ln('master_id')}"):
        if node.text:
            try:
                master_ids.add(int(node.text))
            except ValueError:
                pass

    for node in element.xpath(f".//{ln('artist')}/{ln('id')}"):
        if node.text:
            try:
                artist_ids.add(int(node.text))
            except ValueError:
                pass

    for node in element.xpath(f".//{ln('labels')}/{ln('label')}"):
        value = node.get("id")
        if value:
            try:
                label_ids.add(int(value))
            except ValueError:
                pass

    for node in element.xpath(f".//{ln('companies')}/{ln('company')}/{ln('id')}"):
        if node.text:
            try:
                label_ids.add(int(node.text))
            except ValueError:
                pass

    return master_ids, artist_ids, label_ids


def collect_master_refs(element: etree._Element) -> Tuple[set, set]:
    """Collect main_release and artist ids referenced by a master."""
    release_ids: set = set()
    artist_ids: set = set()

    for node in element.xpath(f"./{ln('main_release')}"):
        if node.text:
            try:
                release_ids.add(int(node.text))
            except ValueError:
                pass

    for node in element.xpath(f".//{ln('artists')}/{ln('artist')}/{ln('id')}"):
        if node.text:
            try:
                artist_ids.add(int(node.text))
            except ValueError:
                pass

    return release_ids, artist_ids


def collect_artist_member_ids(element: etree._Element) -> set:
    """Collect member artist ids embedded in an artist element."""
    member_ids: set = set()
    for node in element.xpath(f".//{ln('members')}/{ln('name')}"):
        value = node.get("id")
        if value:
            try:
                member_ids.add(int(value))
            except ValueError:
                pass
    for node in element.xpath(f".//{ln('members')}/{ln('id')}"):
        if node.text:
            try:
                member_ids.add(int(node.text))
            except ValueError:
                pass
    return member_ids


def reservoir_add(
    reservoir: List[Tuple[int, int, bytes, Dict[str, int]]],
    seen: int,
    capacity: int,
    item: Tuple[int, int, bytes, Dict[str, int]],
    rng: random.Random,
) -> None:
    """Update a reservoir sample in-place."""
    if capacity <= 0:
        return
    if len(reservoir) < capacity:
        reservoir.append(item)
        return
    j = rng.randrange(seen)
    if j < capacity:
        reservoir[j] = item


def select_releases(
    path: Path,
    size: int,
    complexity: str,
    seed: int,
    mixed_ratio: float,
    available_artists: Optional[set],
    available_labels: Optional[set],
    available_masters: Optional[set],
    progress_every: int,
) -> Tuple[Dict[int, bytes], Dict[int, Dict[str, int]], Dict[int, int]]:
    """Select release elements according to complexity or randomness."""
    rng = random.Random(seed)
    if size <= 0:
        return {}, {}, {}

    selected: Dict[int, bytes] = {}
    meta: Dict[int, Dict[str, int]] = {}
    coverage_map: Dict[int, int] = {}

    def coverage_score(element: etree._Element) -> Optional[int]:
        """Compute how many referenced ids are present in available dumps."""
        if (
            available_artists is None
            or available_labels is None
            or available_masters is None
        ):
            return None
        masters, artists, labels = collect_release_refs(element)
        score = 0
        score += len(masters & available_masters)
        score += len(artists & available_artists)
        score += len(labels & available_labels)
        return score

    def push_top(
        heap: List[Tuple[Tuple[int, int], int, bytes, Dict[str, int], int]],
        item: Tuple[Tuple[int, int], int, bytes, Dict[str, int], int],
        limit: int,
    ) -> None:
        if limit <= 0:
            return
        if len(heap) < limit:
            heapq.heappush(heap, item)
        else:
            if item[:2] > heap[0][:2]:
                heapq.heapreplace(heap, item)

    progress = make_progress("Scanning releases (selection)", progress_every)
    if complexity == "random":
        reservoir: List[Tuple[int, int, bytes, Dict[str, int]]] = []
        seen = 0
        for element in iter_entities(path, "release", progress):
            rid = release_id(element)
            if rid is None:
                continue
            counts = release_complexity(element)
            cov = coverage_score(element) or 0
            xml_bytes = etree.tostring(element, encoding="utf-8")
            seen += 1
            reservoir_add(
                reservoir,
                seen,
                size,
                (cov, rid, xml_bytes, counts),
                rng,
            )
        for cov, rid, xml_bytes, counts in reservoir:
            selected[rid] = xml_bytes
            meta[rid] = counts
            coverage_map[rid] = cov
        if progress is not None:
            progress.finish()
        return selected, meta, coverage_map

    if complexity == "mixed":
        top_size = max(1, int(round(size * mixed_ratio)))
        remaining = max(0, size - top_size)
        top_heap: List[Tuple[Tuple[int, int], int, bytes, Dict[str, int], int]] = []
        for element in iter_entities(path, "release", progress):
            rid = release_id(element)
            if rid is None:
                continue
            counts = release_complexity(element)
            cov = coverage_score(element)
            if cov is None:
                score_key = (counts["total"], 0)
                cov = 0
            else:
                score_key = (cov, counts["total"])
            xml_bytes = etree.tostring(element, encoding="utf-8")
            item = (score_key, rid, xml_bytes, counts, cov)
            push_top(top_heap, item, top_size)

        top_ids = {rid for _, rid, _, _, _ in top_heap}
        reservoir: List[Tuple[int, int, bytes, Dict[str, int]]] = []
        seen = 0
        if remaining > 0:
            if progress is not None:
                progress.finish()
            progress = make_progress("Scanning releases (mixed remainder)", progress_every)
            for element in iter_entities(path, "release", progress):
                rid = release_id(element)
                if rid is None or rid in top_ids:
                    continue
                counts = release_complexity(element)
                cov = coverage_score(element) or 0
                xml_bytes = etree.tostring(element, encoding="utf-8")
                seen += 1
                reservoir_add(
                    reservoir,
                    seen,
                    remaining,
                    (cov, rid, xml_bytes, counts),
                    rng,
                )

        for score_key, rid, xml_bytes, counts, cov in top_heap:
            selected[rid] = xml_bytes
            meta[rid] = counts
            coverage_map[rid] = cov
        for cov, rid, xml_bytes, counts in reservoir:
            selected[rid] = xml_bytes
            meta[rid] = counts
            coverage_map[rid] = cov
        if progress is not None:
            progress.finish()
        return selected, meta, coverage_map

    # highest complexity (default)
    top_heap: List[Tuple[Tuple[int, int], int, bytes, Dict[str, int], int]] = []
    for element in iter_entities(path, "release", progress):
        rid = release_id(element)
        if rid is None:
            continue
        counts = release_complexity(element)
        cov = coverage_score(element)
        if cov is None:
            score_key = (counts["total"], 0)
            cov = 0
        else:
            score_key = (cov, counts["total"])
        xml_bytes = etree.tostring(element, encoding="utf-8")
        item = (score_key, rid, xml_bytes, counts, cov)
        push_top(top_heap, item, size)

    for score_key, rid, xml_bytes, counts, cov in top_heap:
        selected[rid] = xml_bytes
        meta[rid] = counts
        coverage_map[rid] = cov
    if progress is not None:
        progress.finish()
    return selected, meta, coverage_map


def extract_releases(
    path: Path,
    target_ids: set,
    existing: Dict[int, bytes],
    meta: Dict[int, Dict[str, int]],
    progress_every: int,
) -> Tuple[set, set, set]:
    """Extract releases by id and collect their cross-file references."""
    new_master_ids: set = set()
    new_artist_ids: set = set()
    new_label_ids: set = set()
    remaining = target_ids - set(existing.keys())
    if not remaining:
        return new_master_ids, new_artist_ids, new_label_ids

    progress = make_progress("Scanning releases (closure)", progress_every)
    for element in iter_entities(path, "release", progress):
        rid = release_id(element)
        if rid is None or rid not in remaining:
            continue
        counts = release_complexity(element)
        meta[rid] = counts
        xml_bytes = etree.tostring(element, encoding="utf-8")
        existing[rid] = xml_bytes
        masters, artists, labels = collect_release_refs(element)
        new_master_ids |= masters
        new_artist_ids |= artists
        new_label_ids |= labels

    if progress is not None:
        progress.finish()
    return new_master_ids, new_artist_ids, new_label_ids


def extract_masters(
    path: Path,
    target_ids: set,
    existing: Dict[int, bytes],
    progress_every: int,
) -> Tuple[set, set]:
    """Extract masters by id and collect referenced releases and artists."""
    new_release_ids: set = set()
    new_artist_ids: set = set()
    remaining = target_ids - set(existing.keys())
    if not remaining:
        return new_release_ids, new_artist_ids

    progress = make_progress("Scanning masters", progress_every)
    for element in iter_entities(path, "master", progress):
        mid = master_id(element)
        if mid is None or mid not in remaining:
            continue
        xml_bytes = etree.tostring(element, encoding="utf-8")
        existing[mid] = xml_bytes
        releases, artists = collect_master_refs(element)
        new_release_ids |= releases
        new_artist_ids |= artists

    if progress is not None:
        progress.finish()
    return new_release_ids, new_artist_ids


def extract_artists(
    path: Path,
    target_ids: set,
    existing: Dict[int, bytes],
    progress_every: int,
) -> set:
    """Extract artists by id and collect member references."""
    new_member_ids: set = set()
    remaining = target_ids - set(existing.keys())
    if not remaining:
        return new_member_ids

    progress = make_progress("Scanning artists", progress_every)
    for element in iter_entities(path, "artist", progress):
        aid = child_id_text(element)
        if aid is None or aid not in remaining:
            continue
        xml_bytes = etree.tostring(element, encoding="utf-8")
        existing[aid] = xml_bytes
        new_member_ids |= collect_artist_member_ids(element)
    if progress is not None:
        progress.finish()
    return new_member_ids


def extract_labels(
    path: Path,
    target_ids: set,
    existing: Dict[int, bytes],
    progress_every: int,
) -> None:
    """Extract labels by id into the output map."""
    remaining = target_ids - set(existing.keys())
    if not remaining:
        return
    progress = make_progress("Scanning labels", progress_every)
    for element in iter_entities(path, "label", progress):
        lid = child_id_text(element)
        if lid is None or lid not in remaining:
            continue
        existing[lid] = etree.tostring(element, encoding="utf-8")
    if progress is not None:
        progress.finish()


def extract_by_id(
    path: Path,
    tag: str,
    target_ids: set,
    existing: Dict[int, bytes],
    progress_every: int,
    *,
    id_attr: Optional[str] = None,
    id_child: Optional[str] = None,
    label: Optional[str] = None,
) -> None:
    """Extract elements by id (attribute or child element)."""
    remaining = target_ids - set(existing.keys())
    if not remaining:
        return
    progress = make_progress(label or f"Scanning {tag}s", progress_every)
    for element in iter_entities(path, tag, progress):
        value: Optional[str] = None
        if id_attr:
            value = element.get(id_attr)
        elif id_child:
            node = element.find(id_child)
            if node is not None:
                value = node.text
        if not value:
            continue
        try:
            entity_id = int(value)
        except ValueError:
            continue
        if entity_id not in remaining:
            continue
        existing[entity_id] = etree.tostring(element, encoding="utf-8")
    if progress is not None:
        progress.finish()


def write_output(
    info: SourceInfo,
    elements_by_id: Dict[int, bytes],
    output_path: Path,
) -> None:
    """Write a fixture XML file with preserved header and root info."""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    preamble = info.preamble_lines
    start_tag, end_tag = build_root_tag(info.root_tag, info.root_attrib, info.nsmap)
    ids = sorted(elements_by_id.keys())
    with output_path.open("wb") as fp:
        for line in preamble:
            fp.write(line.encode("utf-8"))
            fp.write(b"\n")
        fp.write(start_tag.encode("utf-8"))
        fp.write(b"\n")
        for i in ids:
            fp.write(elements_by_id[i])
            fp.write(b"\n")
        fp.write(end_tag.encode("utf-8"))
        fp.write(b"\n")


def find_dump(input_dir: Path, kind: str) -> Path:
    """Locate a dump file by kind, preferring uncompressed XML."""
    patterns = [f"*{kind}*.xml", f"*{kind}*.xml.gz"]
    candidates: List[Path] = []
    for pattern in patterns:
        candidates.extend(sorted(input_dir.glob(pattern)))
    if not candidates:
        raise FileNotFoundError(f"Could not find {kind} dump in {input_dir}")
    # prefer uncompressed
    for p in candidates:
        if p.suffix == ".xml":
            return p
    return candidates[0]


def output_name(source: Path) -> str:
    """Return the output filename for a source path."""
    name = source.name
    if name.endswith(".xml.gz"):
        return name[:-3]
    return name


def build_source_info(path: Path) -> SourceInfo:
    """Load root and preamble details needed to write fixtures."""
    root_tag, root_attrib, nsmap = get_root_info(path)
    return SourceInfo(
        path=path,
        preamble_lines=extract_preamble(path),
        root_tag=root_tag,
        root_attrib=root_attrib,
        nsmap=nsmap,
    )


def collect_available_ids(
    path: Path,
    tag: str,
    id_from_attr: bool,
    progress_every: int,
    label: str,
) -> set:
    """Scan a dump file to gather available ids for coverage scoring."""
    ids: set = set()
    progress = make_progress(label, progress_every)
    for element in iter_entities(path, tag, progress):
        if id_from_attr:
            value = element.get("id")
            if value:
                try:
                    ids.add(int(value))
                except ValueError:
                    pass
        else:
            node = element.find("id")
            if node is not None and node.text:
                try:
                    ids.add(int(node.text))
                except ValueError:
                    pass
    if progress is not None:
        progress.finish()
    return ids


def main(argv: Optional[Sequence[str]] = None) -> int:
    """CLI entry point for fixture generation."""
    parser = argparse.ArgumentParser(description="Generate Discogs XML fixtures")
    parser.add_argument("--input-dir", type=Path, default=Path("tests/samples"))
    parser.add_argument("--output-dir", type=Path, default=Path("tests/fixtures"))
    parser.add_argument(
        "--manifest",
        type=Path,
        help="Reuse an existing manifest.json to select exact IDs",
    )
    parser.add_argument("--artists")
    parser.add_argument("--labels")
    parser.add_argument("--masters")
    parser.add_argument("--releases")
    parser.add_argument("--size", type=int, default=25)
    parser.add_argument(
        "--complexity",
        choices=["highest", "random", "mixed"],
        default="highest",
    )
    parser.add_argument("--seed", type=int, default=1)
    parser.add_argument("--mixed-ratio", type=float, default=0.7)
    parser.add_argument(
        "--availability-scan",
        choices=["auto", "always", "never"],
        default="auto",
        help="Scan artists/labels/masters for available IDs to improve coherence",
    )
    parser.add_argument(
        "--availability-max-mb",
        type=int,
        default=256,
        help="When availability-scan=auto, only scan if total size <= this value",
    )
    parser.add_argument(
        "--progress-every",
        type=int,
        default=50000,
        help="Print progress every N parsed entities (0 to disable)",
    )
    args = parser.parse_args(argv)

    input_dir: Path = args.input_dir
    output_dir: Path = args.output_dir

    artists_path = Path(args.artists) if args.artists else find_dump(input_dir, "artists")
    labels_path = Path(args.labels) if args.labels else find_dump(input_dir, "labels")
    masters_path = Path(args.masters) if args.masters else find_dump(input_dir, "masters")
    releases_path = (
        Path(args.releases) if args.releases else find_dump(input_dir, "releases")
    )

    for p in (artists_path, labels_path, masters_path, releases_path):
        if not p.exists():
            raise FileNotFoundError(f"Missing input file: {p}")

    def mb(path: Path) -> float:
        """Return file size in megabytes for display."""
        return path.stat().st_size / (1024 * 1024)

    print("Input files:", file=sys.stderr)
    print(f"  artists:  {artists_path} ({mb(artists_path):.1f} MB)", file=sys.stderr)
    print(f"  labels:   {labels_path} ({mb(labels_path):.1f} MB)", file=sys.stderr)
    print(f"  masters:  {masters_path} ({mb(masters_path):.1f} MB)", file=sys.stderr)
    print(f"  releases: {releases_path} ({mb(releases_path):.1f} MB)", file=sys.stderr)

    sources = {
        "artists": build_source_info(artists_path),
        "labels": build_source_info(labels_path),
        "masters": build_source_info(masters_path),
        "releases": build_source_info(releases_path),
    }

    manifest_mode = args.manifest is not None
    do_availability_scan = False
    release_meta: Dict[int, Dict[str, int]] = {}
    release_coverage: Dict[int, int] = {}

    if manifest_mode:
        manifest_path = args.manifest
        if not manifest_path.exists():
            raise FileNotFoundError(f"Missing manifest file: {manifest_path}")
        manifest_data = json.loads(manifest_path.read_text(encoding="utf-8"))
        ids = manifest_data.get("ids", {})
        needed_release_ids = set(ids.get("releases", []))
        needed_master_ids = set(ids.get("masters", []))
        needed_artist_ids = set(ids.get("artists", []))
        needed_label_ids = set(ids.get("labels", []))
        seed_release_ids = set(ids.get("seed_releases", needed_release_ids))

        for entry in manifest_data.get("release_complexity", []):
            rid = entry.get("id")
            if rid is None:
                continue
            counts = entry.get("counts")
            if isinstance(counts, dict):
                release_meta[rid] = counts
            coverage = entry.get("coverage")
            if isinstance(coverage, int):
                release_coverage[rid] = coverage

        print(f"Manifest replay: {manifest_path}", file=sys.stderr)
        print(
            "Progress:",
            f"every={args.progress_every} (0 disables)",
            file=sys.stderr,
        )

        release_map: Dict[int, bytes] = {}
        master_map: Dict[int, bytes] = {}
        artist_map: Dict[int, bytes] = {}
        label_map: Dict[int, bytes] = {}

        extract_by_id(
            releases_path,
            "release",
            needed_release_ids,
            release_map,
            args.progress_every,
            id_attr="id",
            label="Scanning releases (manifest)",
        )
        extract_by_id(
            masters_path,
            "master",
            needed_master_ids,
            master_map,
            args.progress_every,
            id_attr="id",
            label="Scanning masters (manifest)",
        )
        extract_by_id(
            artists_path,
            "artist",
            needed_artist_ids,
            artist_map,
            args.progress_every,
            id_child="id",
            label="Scanning artists (manifest)",
        )
        extract_by_id(
            labels_path,
            "label",
            needed_label_ids,
            label_map,
            args.progress_every,
            id_child="id",
            label="Scanning labels (manifest)",
        )
    else:
        total_size_mb = (
            artists_path.stat().st_size
            + labels_path.stat().st_size
            + masters_path.stat().st_size
        ) / (1024 * 1024)
        do_availability_scan = False
        if args.availability_scan == "always":
            do_availability_scan = True
        elif args.availability_scan == "auto":
            do_availability_scan = total_size_mb <= args.availability_max_mb

        print(
            "Availability scan:",
            f"mode={args.availability_scan}",
            f"used={do_availability_scan}",
            f"threshold={args.availability_max_mb} MB",
            file=sys.stderr,
        )
        print(
            "Selection:",
            f"size={args.size}",
            f"complexity={args.complexity}",
            f"seed={args.seed}",
            f"mixed_ratio={args.mixed_ratio}",
            file=sys.stderr,
        )
        print(
            "Progress:",
            f"every={args.progress_every} (0 disables)",
            file=sys.stderr,
        )

        available_artists = None
        available_labels = None
        available_masters = None
        if do_availability_scan:
            available_artists = collect_available_ids(
                artists_path,
                "artist",
                False,
                args.progress_every,
                "Scanning artists (availability)",
            )
            available_labels = collect_available_ids(
                labels_path,
                "label",
                False,
                args.progress_every,
                "Scanning labels (availability)",
            )
            available_masters = collect_available_ids(
                masters_path,
                "master",
                True,
                args.progress_every,
                "Scanning masters (availability)",
            )

        selected_releases, release_meta, release_coverage = select_releases(
            releases_path,
            args.size,
            args.complexity,
            args.seed,
            args.mixed_ratio,
            available_artists,
            available_labels,
            available_masters,
            args.progress_every,
        )
        seed_release_ids = set(selected_releases.keys())

        release_map: Dict[int, bytes] = dict(selected_releases)
        master_map: Dict[int, bytes] = {}
        artist_map: Dict[int, bytes] = {}
        label_map: Dict[int, bytes] = {}

        needed_release_ids: set = set(release_map.keys())
        needed_master_ids: set = set()
        needed_artist_ids: set = set()
        needed_label_ids: set = set()

        # collect refs from seed releases
        for rid, xml_bytes in list(release_map.items()):
            element = etree.fromstring(xml_bytes)
            masters, artists, labels = collect_release_refs(element)
            needed_master_ids |= masters
            needed_artist_ids |= artists
            needed_label_ids |= labels

        # close release/master references
        while True:
            added_any = False

            new_release_ids, new_artist_ids = extract_masters(
                masters_path, needed_master_ids, master_map, args.progress_every
            )
            if new_release_ids or new_artist_ids:
                if new_release_ids - needed_release_ids:
                    needed_release_ids |= new_release_ids
                    added_any = True
                if new_artist_ids - needed_artist_ids:
                    needed_artist_ids |= new_artist_ids
                    added_any = True

            new_master_ids, new_artist_ids, new_label_ids = extract_releases(
                releases_path,
                needed_release_ids,
                release_map,
                release_meta,
                args.progress_every,
            )
            if new_master_ids or new_artist_ids or new_label_ids:
                if new_master_ids - needed_master_ids:
                    needed_master_ids |= new_master_ids
                    added_any = True
                if new_artist_ids - needed_artist_ids:
                    needed_artist_ids |= new_artist_ids
                    added_any = True
                if new_label_ids - needed_label_ids:
                    needed_label_ids |= new_label_ids
                    added_any = True

            if not added_any:
                break

        # close artist member references
        while True:
            new_member_ids = extract_artists(
                artists_path, needed_artist_ids, artist_map, args.progress_every
            )
            if not new_member_ids - needed_artist_ids:
                break
            needed_artist_ids |= new_member_ids

        # labels
        extract_labels(labels_path, needed_label_ids, label_map, args.progress_every)

    output_dir.mkdir(parents=True, exist_ok=True)
    write_output(
        sources["artists"],
        artist_map,
        output_dir / output_name(artists_path),
    )
    write_output(
        sources["labels"],
        label_map,
        output_dir / output_name(labels_path),
    )
    write_output(
        sources["masters"],
        master_map,
        output_dir / output_name(masters_path),
    )
    write_output(
        sources["releases"],
        release_map,
        output_dir / output_name(releases_path),
    )

    if manifest_mode:
        sample_size = len(seed_release_ids)
        complexity_label = "manifest"
        manifest_source = str(args.manifest)
    else:
        sample_size = args.size
        complexity_label = args.complexity
        manifest_source = None

    manifest = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "config": {
            "input_dir": str(input_dir),
            "output_dir": str(output_dir),
            "sample_size": sample_size,
            "complexity": complexity_label,
            "seed": args.seed,
            "mixed_ratio": args.mixed_ratio,
            "availability_scan": args.availability_scan,
            "availability_max_mb": args.availability_max_mb,
            "availability_used": do_availability_scan,
            "manifest_mode": manifest_mode,
            "manifest_path": manifest_source,
            "input_files": {
                "artists": str(artists_path),
                "labels": str(labels_path),
                "masters": str(masters_path),
                "releases": str(releases_path),
            },
        },
        "counts": {
            "seed_releases": len(seed_release_ids),
            "releases": len(release_map),
            "masters": len(master_map),
            "artists": len(artist_map),
            "labels": len(label_map),
        },
        "ids": {
            "seed_releases": sorted(seed_release_ids),
            "releases": sorted(release_map.keys()),
            "masters": sorted(master_map.keys()),
            "artists": sorted(artist_map.keys()),
            "labels": sorted(label_map.keys()),
        },
        "release_complexity": [
            {
                "id": rid,
                "reason": "seed" if rid in seed_release_ids else "closure",
                "counts": release_meta.get(rid, {}),
                "coverage": release_coverage.get(rid, None),
            }
            for rid in sorted(release_map.keys())
        ],
        "missing": {
            "releases": sorted(needed_release_ids - set(release_map.keys())),
            "masters": sorted(needed_master_ids - set(master_map.keys())),
            "artists": sorted(needed_artist_ids - set(artist_map.keys())),
            "labels": sorted(needed_label_ids - set(label_map.keys())),
        },
    }

    manifest_path = output_dir / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")

    print("Wrote fixtures to", output_dir)
    print("Releases:", len(release_map), "Masters:", len(master_map))
    print("Artists:", len(artist_map), "Labels:", len(label_map))
    print("Manifest:", manifest_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
