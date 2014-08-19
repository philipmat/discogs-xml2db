#/bin/bash
#set -xv
USER_AGENT="Mozilla/5.0 (Macintosh; Intel Mac OS X 10_7_2) AppleWebKit/534.51.22 (KHTML, like Gecko) Version/5.1.1 Safari/534.51.22"
ACCEPT="Accept-Encoding: gzip, deflate"
D_URL="http://www.discogs.com/data/"
D_TMP=/tmp/discogs.urls
D_PATTERN="discogs_\d+_(artists|labels|masters|releases).xml.gz"

TEST=""
[[ "$1" == '--test' ]] && TEST='--spider -S'

echo "" > $D_TMP

for f in `wget -c --user-agent="$USER_AGENT" --header="$ACCEPT" -qO- $D_URL | ack-grep -io "$D_PATTERN" | sort | uniq | tail -n 4` ; do
	echo $D_URL$f >> $D_TMP
done

wget -c --user-agent="$USER_AGENT" --header="$ACCEPT" --no-clobber --input-file=$D_TMP $TEST --append-output=$D_TMP.log
