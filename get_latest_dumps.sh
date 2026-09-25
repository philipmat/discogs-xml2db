#!/bin/sh
#set -xv

USER_AGENT="Mozilla/5.0 (compatible; discogs-xml2db/v2.0; +https://github.com/philipmat/discogs-xml2db)"
ACCEPT="Accept-Encoding: gzip, deflate"
D_URL_LIST="https://data.discogs.com/?prefix=data%2F"$(date +"%Y")"%2F"
D_URL_DIR="https://data.discogs.com/data/"$(date +"%Y")"/"
D_TMP=/tmp/discogs.urls
D_PATTERN="discogs_[0-9]{8}_(artists|labels|masters|releases).xml.gz"

WGET=wget
command -v wget2 >/dev/null 2>&1 && WGET=wget2

TEST=""
DOWNLOAD_DIR=""
for arg in "$@"; do
	if [ "$arg" = '--test' ]; then
		TEST='--spider -S'
	else
		DOWNLOAD_DIR=$arg
	fi
done

if [ -n "$DOWNLOAD_DIR" ]; then
	mkdir -p "$DOWNLOAD_DIR" || exit 1
fi

: > "$D_TMP"

$WGET -c --user-agent="$USER_AGENT" --header="$ACCEPT" -O- "$D_URL_LIST" | grep -Eio "$D_PATTERN" | sort | uniq | tail -n 4 | while IFS= read -r f; do
	echo "$D_URL_DIR$f" >> "$D_TMP"
done

if [ ! -s "$D_TMP" ]; then
	echo "No dump files found; check the Discogs dump listing URL or network connection." >&2
	exit 1
fi

if [ -n "$TEST" ]; then
	if [ -n "$DOWNLOAD_DIR" ]; then
		$WGET -c --user-agent="$USER_AGENT" --header="$ACCEPT" --no-clobber --input-file="$D_TMP" -P "$DOWNLOAD_DIR" --spider -S --progress=bar
	else
		$WGET -c --user-agent="$USER_AGENT" --header="$ACCEPT" --no-clobber --input-file="$D_TMP" --spider -S --progress=bar
	fi
else
	if [ -n "$DOWNLOAD_DIR" ]; then
		$WGET -c --user-agent="$USER_AGENT" --header="$ACCEPT" --no-clobber --input-file="$D_TMP" -P "$DOWNLOAD_DIR" --progress=bar
	else
		$WGET -c --user-agent="$USER_AGENT" --header="$ACCEPT" --no-clobber --input-file="$D_TMP" --progress=bar
	fi
fi
