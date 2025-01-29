#!/usr/bin/env python
import argparse
import os
import re
import sys
import time
import urllib
from datetime import date
from urllib.request import urlopen, urlretrieve
from xml.dom import minidom

year = date.today().year
USER_AGENT = (
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_7_2) AppleWebKit/534.51.22 "
    "(KHTML, like Gecko) Version/5.1.1 Safari/534.51.22"
)

ACCEPT = "Accept-Encoding: gzip, deflate"
URL_LIST = "http://discogs-data.s3-us-west-2.amazonaws.com/?delimiter=/&prefix=data/{0}/".format(
    year
)
URL_DIR = "http://discogs-data.s3-us-west-2.amazonaws.com/{0}"
TMP = "discogs-{0}.urls".format(year)
PATTERN = r"discogs_[0-9]{8}_(artists|labels|masters|releases|CHECKSUM)\.(xml.gz|txt)"


def getText(nodelist):
    rc = []
    for node in nodelist:
        if node.nodeType == node.TEXT_NODE:
            rc.append(node.data)
    return "".join(rc)


def get_list() -> str:
    f = urlopen(URL_LIST)
    content = f.read()
    return content


def xml_extract_latest(text):
    dom = minidom.parseString(text)
    file_nodes = [getText(n.childNodes) for n in dom.getElementsByTagName("Key")]
    files = sorted(file_nodes, reverse=True)
    last_entries = []
    for f in files:
        if re.search(PATTERN, f) is not None:
            last_entries.append(f)
        # 4 files + checksum.
        # brittle approach as it relies on all of them to be in order
        if len(last_entries) == 5:
            break

    return last_entries


def make_url(*chunks):
    for chunk in chunks:
        yield URL_DIR.format(chunk)


def reporthook(count, block_size, total_size):
    global start_time
    if count == 0:
        start_time = time.time()
        return
    duration = time.time() - start_time
    progress_size = int(count * block_size)
    speed = int(progress_size / (1024 * duration))
    percent = int(count * block_size * 100 / total_size)
    sys.stdout.write(
        "\r...%d%%, %d MB / %d MB, %d KB/s, %d seconds passed"
        % (percent, progress_size / (1024 * 1024), total_size / (1024 * 1024), speed, duration)
    )
    sys.stdout.flush()


def save_url(url, filename):
    # print(f"Would retrieve {filename} from {url}")
    # return
    print(f"\nRetrieving {url}")
    urlretrieve(url, filename, reporthook)


def save_urls(dir, *urls):
    if not os.path.exists(dir):
        os.makedirs(dir, exist_ok=True)

    for url in urls:
        _, file = url.rsplit("/", 1)
        file = os.path.join(dir, file)
        save_url(url, file)


def main(args):
    xml = ""
    if len(sys.argv) > 1 and sys.argv[1] == "--test":
        with open(os.path.realpath("./tmp/ListBucketResult.xml")) as fxml:
            xml = fxml.read()
    else:
        xml = get_list()

    urls = list(make_url(*xml_extract_latest(xml)))

    if args.save_urls is not None:
        with open(args.save_urls, mode="w") as f:
            f.writelines(url + os.linesep for url in urls)
        print(f"Wrote urls to {args.save_urls}")

    if args.url_only:
        # if there's no save_urls file, print to std out
        if args.save_urls is None:
            print("\n".join(urls))
        sys.exit(0)

    # if we got this far, we are interested in the actual download
    save_urls(args.output_dir, *urls)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Retrieves the latest discogs xml dumps.")
    parser.add_argument(
        "--url-only",
        "-u",
        action="store_true",
        help="Only retrieve the discogs URLs, does not retrieve the files",
    )
    parser.add_argument("--save-urls", "-s", help="Saves urls to a file")
    parser.add_argument("output_dir", nargs="?", default=os.getcwd())

    args = parser.parse_args()
    main(args)
