#!/bin/sh
#set -xv

USER_AGENT="Mozilla/5.0 (compatible; discogs-xml2db/v2.0; +https://github.com/philipmat/discogs-xml2db)"
ACCEPT="Accept-Encoding: gzip, deflate"
D_YEAR=$(date +"%Y")
D_URL_LIST="https://data.discogs.com/?prefix=data%2F${D_YEAR}%2F"
D_URL_DIR="https://data.discogs.com/?download=data%2F${D_YEAR}%2F"
D_TMP=/tmp/discogs.urls
D_TMP_SIZES=/tmp/discogs.sizes
D_TMP_LIST=/tmp/discogs.list
D_TMP_LOG=/tmp/discogs.log
TAB=$(printf '\t')
D_PATTERN="discogs_[0-9]{8}_(artists|labels|masters|releases)\.xml\.gz"

# Force the progress bar even when output isn't a terminal; wget and wget2 spell this differently.
# wget2 downloads up to 5 files in parallel by default; data.discogs.com throttles concurrent
# connections from the same IP, so keep it to one.
WGET=wget
WGET_PROGRESS="--progress=bar:force"
WGET_THREADS=""
if command -v wget2 >/dev/null 2>&1; then
	WGET=wget2
	WGET_PROGRESS="--progress=bar --force-progress"
	WGET_THREADS="--max-threads=1"
fi
# If a dump download gets rate limited, back off (up to 60s between tries) instead of failing.
WGET_RETRY="--tries=5 --waitretry=60 --retry-on-http-error=429"

WGET=wget
command -v wget2 >/dev/null 2>&1 && WGET=wget2

TEST=""
DOWNLOAD_DIR="."
for arg in "$@"; do
	if [ "$arg" = '--test' ]; then
		TEST=1
	else
		DOWNLOAD_DIR=$arg
	fi
done

if [ -z "$TEST" ]; then
	mkdir -p "$DOWNLOAD_DIR" || exit 1
fi

: > "$D_TMP"

$WGET --user-agent="$USER_AGENT" --header="$ACCEPT" -S --content-on-error -O "$D_TMP_LIST" "$D_URL_LIST" > "$D_TMP_LOG" 2>&1
WGET_STATUS=$?

if grep -Eq '^[[:space:]]*HTTP/[0-9.]+ 429' "$D_TMP_LOG" || gzip -dcf < "$D_TMP_LIST" 2>/dev/null | grep -q 'making requests too quickly'; then
	RETRY_AFTER=$(sed -nE 's/^[[:space:]]*retry-after:[[:space:]]*([^[:space:]]+).*/\1/Ip' "$D_TMP_LOG" | head -n 1)
	{
		echo "data.discogs.com is rate limiting requests from this IP (HTTP 429: \"You are making requests too quickly.\")."
		if [ -n "$RETRY_AFTER" ]; then
			echo "The server asked to retry after: $RETRY_AFTER (seconds or date)."
		else
			echo "Wait a few minutes before trying again; the server did not say for how long."
		fi
		echo "Make sure nothing else on this network (other scripts, browsers, machines behind the same public IP)"
		echo "is downloading from data.discogs.com at the same time; concurrent downloads from one IP are what Discogs throttles."
	} >&2
	exit 1
fi

if [ "$WGET_STATUS" -ne 0 ]; then
	echo "Failed to fetch the dump listing from $D_URL_LIST (wget exit code $WGET_STATUS):" >&2
	tail -n 20 "$D_TMP_LOG" >&2
	exit 1
fi

# Listing rows look like: <date> <time>   <size> <unit>   <a href="...">discogs_..._releases.xml.gz</a>
# Extract "<file>\t<size> <unit>" for the latest dump of each type.
gzip -dcf < "$D_TMP_LIST" \
	| sed -nE "s#^[[:space:]]*[0-9-]+ [0-9:]+[[:space:]]+([0-9.]+ [KMGT]?B)[[:space:]]+<a [^>]*>($D_PATTERN)</a>.*#\2${TAB}\1#p" \
	| sort -u | tail -n 4 > "$D_TMP_SIZES"

cut -f 1 "$D_TMP_SIZES" | while IFS= read -r f; do
	echo "$D_URL_DIR$f" >> "$D_TMP"
done

if [ ! -s "$D_TMP" ]; then
	echo "No dump files found; check the Discogs dump listing URL or network connection." >&2
	exit 1
fi

if [ -n "$TEST" ]; then
	# Sizes come from the listing page; no requests are made for the dump files themselves.
	echo "Would download:"
	while IFS="$TAB" read -r f size; do
		printf '  %10s  %s%s\n' "$size" "$D_URL_DIR" "$f"
	done < "$D_TMP_SIZES"
else
	$WGET -c --user-agent="$USER_AGENT" --header="$ACCEPT" --no-clobber --content-disposition --input-file="$D_TMP" -P "$DOWNLOAD_DIR" $WGET_THREADS $WGET_RETRY $WGET_PROGRESS
fi
