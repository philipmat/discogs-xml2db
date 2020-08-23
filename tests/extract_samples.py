"""
Extracts samples from discogs XML files.

Usage:
  extract_records.py [--count=<count>] FILE

Options:
  --count=<count>  Number of records to be extracted
                   [default: 1000]

"""

import sys
import requests
import gzip
import pathlib
import lxml.etree as etree
import os

from docopt import docopt
from tqdm import tqdm

arguments = docopt(__doc__)

rough_counts = {
    "artists": 5000000,
    "labels": 1100000,
    "masters": 1250000,
    "releases": 8500000,
}

headers = {"User-Agent": "discogs-xml2db/1.0 +https://github.com/philipmat/discogs-xml2db/"}
response = requests.get("https://api.discogs.com/", timeout=5, headers=headers)
try:
    rough_counts.update(response.json().get("statistics"))
except Exception:
    pass

in_file = arguments["FILE"]
try:
    parser_name, max_records = next((x, rough_counts[x]) for x in rough_counts if x in in_file)
except StopIteration:
    print(f"Unable to figure out what kind of file {in_file} is.")


# process: for every 10% of the file, we extract extract_count / 10
SAMPLES = 10
percent_breaks = max_records // SAMPLES
extract_count = int(arguments["--count"])
extract_batch = extract_count // SAMPLES
extract_windows = [
    (percent_breaks * step, percent_breaks * step + extract_batch) for step in range(0, SAMPLES)
]

# since we run this as a script, we need to add the parent folder
# so we can import discogsxml2db from it

parent_path = str(pathlib.Path(__file__).absolute().parent.parent)
sys.path.insert(1, parent_path)
from discogsxml2db.parser import (
    DiscogsArtistParser,
    DiscogsLabelParser,
    DiscogsMasterParser,
    DiscogsReleaseParser,
)  # noqa

_parsers = {
    "artists": {"tag": "artist", "id_method": lambda el: el.find("id")},
    "labels": {"tag": "label", "id_method": lambda el: el.find("id")},
    "masters": {"tag": "master", "id_method": lambda el: el.get("id")},
    "releases": {"tag": "release", "id_method": lambda el: el.get("id")},
}
parser = _parsers[parser_name]


def openfile(fpath: str):
    if fpath.endswith(".gz"):
        return gzip.GzipFile(fpath)
    elif fpath.endswith(".xml"):
        return open(fpath, mode="rb")
    else:
        raise RuntimeError("unknown file type: {}".format(fpath))


def in_extraction_window(pos: int) -> bool:
    for min_x, max_x in extract_windows:
        if min_x <= pos and pos < max_x:
            return True
        if min_x > pos:
            return False
    return False


out_file = os.path.splitext(in_file)[0] + f"-sample-{extract_count}.xml"
in_fp = openfile(in_file)
with open(out_file, mode="wb") as out_fp:
    out_fp.write(b"<" + bytearray(parser_name, "utf-8") + b">\n")
    try:
        inner_pbar = tqdm(
            total=extract_count, desc="Extracting records", unit="records", position=1
        )
        with tqdm(total=max_records, desc="Processing records", unit="records", position=0) as pbar:
            parse_count = 0
            for _, element in etree.iterparse(in_fp, tag=parser["tag"]):
                e_id = parser["id_method"](element)
                if e_id is not None:
                    if in_extraction_window(parse_count):
                        # inner_pbar.write(f"cnt = {cnt}")
                        out_fp.write(etree.tostring(element))
                        inner_pbar.update()
                    pbar.update()
                    parse_count += 1
                # clear element to preserve memory
                element.clear()
        inner_pbar.close()
    except Exception as ex:
        print(ex)
    finally:
        in_fp.close()
        out_fp.write(b"</" + bytearray(parser_name, "utf-8") + b">")

print(f"Wrote {extract_count}/{parse_count} {parser_name} to {out_file}.")
