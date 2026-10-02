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
[ -z "$(git status --porcelain --untracked-files=no)" ] || { echo 'Commit tracked changes before packaging' >&2; exit 2; }
[ -z "$(git ls-files --others --exclude-standard -- src tests docs tools art unity)" ] || { echo 'Commit new project files before packaging' >&2; exit 2; }

echo "==> tests ($CHANNEL)"
DOTNET_GCHeapHardLimit=${DOTNET_GCHeapHardLimit:-0x40000000} dotnet test GenesisUI.sln -c "$CHANNEL" --nologo -v q

VERSION=$(sed -n 's/.*const string Version = "\([^"]*\)".*/\1/p' src/GenesisUI/PluginInfo.cs)
PREVIEW=$(sed -n 's/.*const int PreviewNumber = \([0-9]*\).*/\1/p' src/GenesisUI/PluginInfo.cs)
DLL="src/GenesisUI/bin/$CHANNEL/net48/GenesisUI.dll"
[ -f "$DLL" ] || { echo "missing $DLL" >&2; exit 1; }
STAMP=$(dotnet run --project tools/review -- stamp "$ROOT" "$DLL")
python3 - "$STAMP" "$VERSION" "$PREVIEW" "$CHANNEL" "$(git rev-parse --short=7 HEAD)" <<'PY'
import json, sys
stamp = json.loads(sys.argv[1])
if (stamp['version'], str(stamp['preview']), stamp['channel'], stamp['sha']) != tuple(sys.argv[2:]):
    sys.exit('Compiled package source/version/channel mismatch')
PY

# The Core must be merged, never shipped beside the plugin (see ILRepack.targets).
grep -aq "FaultRegistry" "$DLL" || { echo "GenesisUI.Core was not merged into $DLL" >&2; exit 1; }
grep -aq "SendConfigsAfterLogin" "$DLL" || { echo "ServerSync was not merged into $DLL (D-031)" >&2; exit 1; }

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
[ "$(ls "$OUTDIR"/fonts/*.ttf 2>/dev/null | wc -l)" -eq 6 ] || { echo "expected 6 fonts in $OUTDIR/fonts (run tools/art/fonts.py; Noto Sans Runic is shipped as is)" >&2; exit 1; }
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
python3 tools/shaders/provenance.py "$OUTDIR/art/genesisui.shaders"
cp "$OUTDIR/art/genesisui.shaders" "$STAGE/plugins/art/"
cp art/shaders/provenance.json "$STAGE/plugins/art/shader-provenance.json"
# The store page is written for players (store/README.md); the repository README is for GitHub.
cp icon.png CHANGELOG.md LICENSE "$STAGE/"
cp store/README.md "$STAGE/README.md"

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
  "description": "Valheim's whole interface redrawn in fine gold metal: living health bars, a clear inventory and crafting, a framed map with new markers. Turn any part off; it repairs itself if something breaks.",
  "dependencies": ["ValheimModding-Jotunn-$JOTUNN_VERSION"]
}
JSON

python3 - "$STAGE" "$CHANNEL" "$VERSION" "$PREVIEW" <<'PY'
import hashlib, json, pathlib, subprocess, sys
stage = pathlib.Path(sys.argv[1])
digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
refs = {name + '.dll': digest(pathlib.Path('ref') / (name + '.dll'))
        for name in ('assembly_valheim', 'assembly_utils', 'assembly_guiutils', 'gui_framework', 'Jotunn')}
files = {p.relative_to(stage).as_posix(): digest(p) for p in stage.rglob('*') if p.is_file() and p.name != 'manifest.json'}
evidence = dict(schema=1, commit=subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip(),
                channel=sys.argv[2], version=sys.argv[3], preview=sys.argv[4], references=refs, filesSha256=files)
(stage / 'build-evidence.json').write_text(json.dumps(evidence, indent=2), encoding='utf-8')
PY

python3 - "$STAGE" "dist/$NAME.zip" <<'PY'
import hashlib, os, sys, zipfile
src, dst = sys.argv[1], sys.argv[2]
expected = {}
with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED) as z:
    for root, _, files in os.walk(src):
        for f in sorted(files):
            p = os.path.join(root, f)
            name = os.path.relpath(p, src).replace(os.sep, '/')
            expected[name] = hashlib.sha256(open(p, 'rb').read()).digest()
            z.write(p, name)
with zipfile.ZipFile(dst) as z:
    if len(z.namelist()) != len(expected) or set(z.namelist()) != set(expected) or z.testzip():
        sys.exit('Archive entries/CRC mismatch')
    for name, digest in expected.items():
        if hashlib.sha256(z.read(name)).digest() != digest:
            sys.exit('Archive content hash mismatch: ' + name)
print(f'   verified {len(expected)} archive entries by SHA256')
PY
rm -rf "$STAGE"
echo "==> dist/$NAME.zip ($(stat -c%s "dist/$NAME.zip") bytes)"
sha256sum "dist/$NAME.zip"
