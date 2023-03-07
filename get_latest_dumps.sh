#/bin/bash
#set -xv

USER_AGENT="Mozilla/5.0 (Macintosh; Intel Mac OS X 10_7_2) AppleWebKit/534.51.22 (KHTML, like Gecko) Version/5.1.1 Safari/534.51.22"
ACCEPT="Accept-Encoding: gzip, deflate"
D_URL_LIST="http://discogs-data-dumps.s3-us-west-2.amazonaws.com/?delimiter=/&prefix=data/"$(date +"%Y")"/"
D_URL_DIR="http://discogs-data-dumps.s3-us-west-2.amazonaws.com/data/"$(date +"%Y")"/"
D_TMP=/tmp/discogs.urls
D_PATTERN="discogs_[0-9]{8}_(artists|labels|masters|releases).xml.gz"

TEST=""
[[ "$1" == '--test' ]] && TEST='--spider -S'

echo "" > $D_TMP

for f in `wget -c --user-agent="$USER_AGENT" --header="$ACCEPT" -qO- $D_URL_LIST | grep -Eio "$D_PATTERN" | sort | uniq | tail -n 4` ; do
	echo $D_URL_DIR$f >> $D_TMP
done

wget -c --user-agent="$USER_AGENT" --header="$ACCEPT" --no-clobber --input-file=$D_TMP $TEST --progress=bar
