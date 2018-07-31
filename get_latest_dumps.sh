#/bin/bash
#set -xv

ACCEPT="Accept-Encoding: gzip, deflate"
D_URL_LIST="http://discogs-data.s3-us-west-2.amazonaws.com/?delimiter=/&prefix=data/"$(date +"%Y")"/"
D_URL_DIR="http://discogs-data.s3-us-west-2.amazonaws.com/data/"$(date +"%Y")"/"
D_TMP=/tmp/discogs.urls
D_PATTERN="discogs_[0-9]{8}_(artists|labels|masters|releases).xml.gz"

TEST=""
[[ "$1" == '--test' ]] && TEST='--spider -S'

echo "" > $D_TMP

for f in `wget -c --header="$ACCEPT" -qO- $D_URL_LIST | grep -Eio "$D_PATTERN" | sort | uniq | tail -n 4` ; do
	echo $D_URL_DIR$f >> $D_TMP
done

cat $D_TMP | xargs -n 1 -P 99 wget -c --header="$ACCEPT" --no-clobber $TEST --progress=bar
