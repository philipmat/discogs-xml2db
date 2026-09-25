#!/usr/bin/env bash

# load database config
scriptdir=$(dirname "$(realpath $0)")
source "$scriptdir/mysql.conf"

# execute SQL statements passed to stdin
MYSQL_PWD=$password mysql -h "$host" -P "$port" -u "$user" "$database" < /dev/stdin
