"""Builds the static, Latin-subset fonts GenesisUI ships (docs/ART-DIRECTION.md §3).

    tools/.venv/bin/python tools/art/fonts.py

Downloads the official variable fonts from the Google Fonts repository (SIL OFL 1.1),
verifies their SHA-256, instances the weights the art direction asks for and keeps
only the characters we render (Latin, Latin-1, Latin Extended-A, common punctuation):
pt-BR and English with room for other Latin languages. Outputs go to art/fonts/,
next to each family's OFL.txt. The variable sources are cached in art/fonts/src/
(gitignored).
"""
import hashlib
import os
import sys
import urllib.request

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "art", "fonts", "src")
OUT = os.path.join(ROOT, "art", "fonts")
BASE = "https://raw.githubusercontent.com/google/fonts/main/ofl"

SOURCES = {
    "Cinzel-VF.ttf": (f"{BASE}/cinzel/Cinzel%5Bwght%5D.ttf",
                      "f4d83d34d1f6c741193e4acf4b3dff9531e5a67b6aa65228d00a7db72a4e0f34"),
    "CormorantGaramond-VF.ttf": (f"{BASE}/cormorantgaramond/CormorantGaramond%5Bwght%5D.ttf",
                                 "b20b7d9626dd956b2c5e558692ad328b1f19e3275e2782db4fa07670d83f35e0"),
    "CormorantGaramond-Italic-VF.ttf": (f"{BASE}/cormorantgaramond/CormorantGaramond-Italic%5Bwght%5D.ttf",
                                        "0f48ea6abb2084537854f7174c470991a463b13036309e3b50a81511611c530d"),
}

LICENSES = {
    "Cinzel-OFL.txt": f"{BASE}/cinzel/OFL.txt",
    "CormorantGaramond-OFL.txt": f"{BASE}/cormorantgaramond/OFL.txt",
}

# (variable source, weight, output file)
INSTANCES = [
    ("Cinzel-VF.ttf", 600, "Cinzel-SemiBold.ttf"),
    ("Cinzel-VF.ttf", 500, "Cinzel-Medium.ttf"),
    ("CormorantGaramond-VF.ttf", 500, "CormorantGaramond-Medium.ttf"),
    ("CormorantGaramond-VF.ttf", 600, "CormorantGaramond-SemiBold.ttf"),
    ("CormorantGaramond-Italic-VF.ttf", 500, "CormorantGaramond-MediumItalic.ttf"),
]

UNICODES = (
    list(range(0x20, 0x7F))        # Basic Latin
    + list(range(0xA0, 0x180))     # Latin-1 Supplement + Latin Extended-A
    + list(range(0x2010, 0x2028))  # dashes, quotes, bullet, ellipsis
    + list(range(0x2030, 0x203B))  # per mille, guillemets
    + [0x20AC, 0x2122, 0x2190, 0x2191, 0x2192, 0x2193, 0x25C6, 0x2022]  # € ™ arrows ◆ •
)


def fetch(name, url, sha=None):
    path = os.path.join(SRC, name)
    if not os.path.exists(path):
        print(f"download {name}")
        urllib.request.urlretrieve(url, path)
    if sha:
        digest = hashlib.sha256(open(path, "rb").read()).hexdigest()
        if digest != sha:
            os.remove(path)
            sys.exit(f"{name}: SHA-256 {digest} does not match the pinned {sha}")
    return path


def main():
    os.makedirs(SRC, exist_ok=True)
    for name, (url, sha) in SOURCES.items():
        fetch(name, url, sha)
    for name, url in LICENSES.items():
        with open(fetch(name, url), "rb") as f:
            data = f.read()
        with open(os.path.join(OUT, name), "wb") as f:
            f.write(data)

    options = subset.Options()
    options.layout_features = ["kern", "liga", "lnum", "onum", "tnum", "pnum", "case"]
    options.name_IDs = ["*"]
    options.notdef_outline = True

    for src, weight, out in INSTANCES:
        font = TTFont(os.path.join(SRC, src))
        # Not every family lists every weight in its STAT table (Cinzel lacks 600), so
        # names are set here instead of by the instancer. TMP loads by file path anyway.
        static = instancer.instantiateVariableFont(font, {"wght": weight}, updateFontNames=False)
        style = out[:-4].split("-", 1)[1]
        family = static["name"].getDebugName(1)
        static["name"].setName(style, 2, 3, 1, 0x409)
        static["name"].setName(f"{family} {style}", 4, 3, 1, 0x409)
        static["name"].setName(f"{family.replace(' ', '')}-{style}", 6, 3, 1, 0x409)
        sub = subset.Subsetter(options)
        sub.populate(unicodes=UNICODES)
        sub.subset(static)
        path = os.path.join(OUT, out)
        static.save(path)
        print(f"{out}: {os.path.getsize(path) // 1024} KB")


if __name__ == "__main__":
    main()
