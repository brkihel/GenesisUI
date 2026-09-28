#!/bin/sh
# Fills ref/ with the exact assemblies the plugin compiles against and the
# contract tests read. ref/ is gitignored: these files belong to Iron Gate,
# Unity, BepInEx and Jotunn, and are never redistributed.
#
#   tools/fill-ref.sh [path/to/devplugins/referencias]
#
# The default source is the genesisheim devplugins copy of the production
# server (refresh it after every game, BepInEx or Jotunn update). Compiling
# against the same Jotunn.dll that runs is deliberate: a version mismatch only
# shows up as MissingMethodException at load time.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
SRC=${1:-$ROOT/../devplugins/referencias}
MANAGED="$SRC/valheim/valheim_Data/Managed"
CORE="$SRC/bepinex/core"
JOTUNN="$SRC/bepinex/plugins/ValheimModding-Jotunn/Jotunn.dll"

[ -d "$MANAGED" ] || { echo "missing $MANAGED (run devplugins/atualizar-referencias.sh)" >&2; exit 2; }
[ -f "$JOTUNN" ] || { echo "missing $JOTUNN" >&2; exit 2; }

DEST="$ROOT/ref"
mkdir -p "$DEST"

for f in assembly_valheim assembly_utils assembly_guiutils gui_framework \
         UnityEngine UnityEngine.CoreModule UnityEngine.UI UnityEngine.UIModule \
         UnityEngine.IMGUIModule UnityEngine.InputLegacyModule UnityEngine.TextRenderingModule \
         UnityEngine.TextCoreFontEngineModule UnityEngine.TextCoreTextEngineModule \
         UnityEngine.AssetBundleModule UnityEngine.ImageConversionModule UnityEngine.JSONSerializeModule \
         Unity.TextMeshPro Unity.InputSystem \
         mscorlib netstandard System System.Core; do
    cp "$MANAGED/$f.dll" "$DEST/"
done
cp "$CORE/BepInEx.dll" "$CORE/0Harmony.dll" "$DEST/"
cp "$JOTUNN" "$DEST/"
cp "$SRC/VERSAO.txt" "$DEST/SOURCE.txt" 2>/dev/null || true

echo "ref/ filled from $SRC"
cat "$DEST/SOURCE.txt" 2>/dev/null || true
