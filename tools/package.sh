#!/bin/sh
# Builds GenesisUI in the given channel and lays out the Hexium package.
#
#   tools/package.sh [Preview|Release]      (default: Preview)
#
# Output: dist/GenesisMods-GenesisUI-<version>[-preview.N].zip
#   manifest.json, icon.png (256x256), README.md, CHANGELOG.md, LICENSE at the root;
#   plugins/GenesisUI.dll and plugins/Translations/ below (Jotunn loads
#   Translations/<Language>/*.json from the plugin folder).
# A Preview zip is for Diego's client tests only: never published, never installed
# on a server (docs/RELEASE.md).
set -eu

# Jotunn the plugin is compiled against and depends on (docs/DECISIONS.md D-017).
JOTUNN_VERSION=2.30.2

ROOT=$(cd "$(dirname "$0")/.." && pwd)
CHANNEL=${1:-Preview}
case "$CHANNEL" in Preview|Release) ;; *) echo "channel must be Preview or Release" >&2; exit 2 ;; esac
cd "$ROOT"

[ -f ref/assembly_valheim.dll ] || { echo "ref/ is empty: run tools/fill-ref.sh" >&2; exit 2; }

echo "==> tests ($CHANNEL)"
DOTNET_GCHeapHardLimit=${DOTNET_GCHeapHardLimit:-0x40000000} dotnet test GenesisUI.sln -c "$CHANNEL" --nologo -v q

VERSION=$(sed -n 's/.*const string Version = "\([^"]*\)".*/\1/p' src/GenesisUI/PluginInfo.cs)
PREVIEW=$(sed -n 's/.*const int PreviewNumber = \([0-9]*\).*/\1/p' src/GenesisUI/PluginInfo.cs)
DLL="src/GenesisUI/bin/$CHANNEL/net48/GenesisUI.dll"
[ -f "$DLL" ] || { echo "missing $DLL" >&2; exit 1; }

# The Core must be merged, never shipped beside the plugin (see ILRepack.targets).
grep -aq "FaultRegistry" "$DLL" || { echo "GenesisUI.Core was not merged into $DLL" >&2; exit 1; }

# Icon: Hexium rejects anything but exactly 256x256.
python3 - <<'PY'
import struct, sys
with open("icon.png", "rb") as f:
    head = f.read(24)
w, h = struct.unpack(">II", head[16:24])
if (w, h) != (256, 256):
    sys.exit(f"icon.png is {w}x{h}, Hexium requires 256x256")
PY

if [ "$CHANNEL" = "Preview" ]; then NAME="GenesisMods-GenesisUI-$VERSION-preview.$PREVIEW"; else NAME="GenesisMods-GenesisUI-$VERSION"; fi
STAGE="dist/$NAME"
rm -rf "$STAGE" "dist/$NAME.zip"
mkdir -p "$STAGE/plugins"

OUTDIR=$(dirname "$DLL")
# Fonts (OFL, with their licenses) and rendered art ship next to the DLL.
[ "$(ls "$OUTDIR"/fonts/*.ttf 2>/dev/null | wc -l)" -eq 5 ] || { echo "expected 5 fonts in $OUTDIR/fonts (run tools/art/fonts.py)" >&2; exit 1; }
[ -f "$OUTDIR/art/sprites.json" ] || { echo "missing $OUTDIR/art/sprites.json (run tools/art/render.py)" >&2; exit 1; }

cp "$DLL" "$STAGE/plugins/"
cp -r src/GenesisUI/Translations "$OUTDIR/fonts" "$STAGE/plugins/"
# Only the files named in the art manifest are copied (stale PNGs in the build output never ship).
python3 - "$OUTDIR/art" "$STAGE/plugins/art" <<'PY'
import json, os, shutil, sys
src, dst = sys.argv[1:]
os.makedirs(dst, exist_ok=True)
with open(os.path.join(src, "sprites.json"), encoding="utf-8") as f:
    manifest = json.load(f)
shutil.copy2(os.path.join(src, "sprites.json"), dst)
for sprite in manifest["sprites"]:
    name = sprite["file"]
    if os.path.basename(name) != name:
        sys.exit("sprite file must be a filename: " + name)
    shutil.copy2(os.path.join(src, name), os.path.join(dst, name))
print(f"   art: {len(manifest['sprites'])} sprites")
PY
cp icon.png README.md CHANGELOG.md LICENSE "$STAGE/"

# The manifest dependency must name the Jotunn we compiled against (ref/Jotunn.dll).
python3 - "$JOTUNN_VERSION" <<'PY'
import sys
want = sys.argv[1]
data = open("ref/Jotunn.dll", "rb").read()
if want.encode("utf-16-le") not in data:
    sys.exit(f"ref/Jotunn.dll is not Jotunn {want}: update JOTUNN_VERSION in tools/package.sh")
PY

cat > "$STAGE/manifest.json" <<JSON
{
  "name": "GenesisUI",
  "version_number": "$VERSION",
  "website_url": "https://github.com/brkihel/GenesisUI",
  "description": "Modular, themed Valheim UI built for security and stability first. Client-only and visual: it never moves your items or talks to the server.",
  "dependencies": ["ValheimModding-Jotunn-$JOTUNN_VERSION"]
}
JSON

python3 - "$STAGE" "dist/$NAME.zip" <<'PY'
import os, sys, zipfile
src, dst = sys.argv[1], sys.argv[2]
with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED) as z:
    for root, _, files in os.walk(src):
        for f in sorted(files):
            p = os.path.join(root, f)
            z.write(p, os.path.relpath(p, src))
    for n in sorted(z.namelist()):
        print("   " + n)
PY
rm -rf "$STAGE"
echo "==> dist/$NAME.zip ($(stat -c%s "dist/$NAME.zip") bytes)"
sha256sum "dist/$NAME.zip"
