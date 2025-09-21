#!/usr/bin/env bash

# execute mysql queries
mysql_query () {
    sql=$1
    MYSQL_PWD=$password mysql -h "$host" -P "$port" -u "$user" -e "$sql" $database
}

# import a csv file into mysql
importcsv () {
    path="$1"
    filename=$(basename "$path")
    table="$(echo $filename | sed 's/\.csv.*//')"
    sql="load data local infile '/dev/stdin'
         into table \`$table\`
         fields terminated by ',' ESCAPED BY '' OPTIONALLY ENCLOSED BY '\"'
         lines terminated by '\r\n'
         ignore 1 lines"
    if [[ "$filename" == *.csv.bz2 ]]; then
        cols="$(cat $path | bunzip2 | head -n 1)"
        cat_or_pv "$path" "$filename" | bunzip2 | mysql_query "$sql ($cols);"
    elif [[ "$filename" == *.csv ]]; then
        cols="$(cat $path | head -n 1)"
        cat_or_pv "$path" "$filename" | mysql_query "$sql ($cols);"
    else
        echo "error: $filename is not a .csv or .csv.bz2 file, skipping"
    fi
}

# display usage manual if no files are specified
if [ "$#" -eq 0 ]; then
    echo "usage: "
    echo "  mysql_loadcsv table.csv table2.csv.bz2"
    exit 1
fi

# load database config
scriptdir=$(dirname "$(realpath $0)")
source "$scriptdir/mysql.conf"

# use pv to show progress if available
if ! [ -x "$(command -v pv)" ]; then
    echo "warning: 'pv' command not found, import progress will not be displayed"
    cat_or_pv() { echo "importing $2" 1>&2 ; cat $1; }
else
    cat_or_pv() { pv "$1" --name "$2"; }
fi

# import csv files passed as arguments
for path in "$@"; do
    if [[ -f "$path" && -r "$path" ]]; then
        importcsv $path
    else
        echo "error: '$path' is not a readable file"
        exit 1
    fi
done
