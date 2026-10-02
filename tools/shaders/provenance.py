"""Read-only shader provenance gate for the POSIX package path (standard library only)."""
import hashlib
import json
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[2]
record = json.loads((root / 'art/shaders/provenance.json').read_text(encoding='utf-8-sig'))
bundle = Path(sys.argv[1])
digest = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
if (record.get('schema') != 1 or record.get('requiredShader') != 'GenesisUI/Keyed'
        or record.get('unityVersion') != '6000.0.75f1'
        or record.get('target') != 'StandaloneWindows64'
        or not bundle.is_file() or record.get('bundleSha256') != digest(bundle)):
    sys.exit('Required shader provenance/bundle mismatch')
gpu = record.get('gpu', {})
if (gpu.get('api'), gpu.get('solid'), gpu.get('edge'), gpu.get('clear')) != ('Direct3D11', 256, 512, 256):
    sys.exit('Required keyed GPU provenance missing')
if gpu.get('orbitPhases') != 4:
    sys.exit('Required equip orbit GPU provenance missing')
sources = {p.relative_to(root).as_posix(): digest(p)
           for p in (root / 'unity/Assets/GenesisUI').rglob('*') if p.is_file()}
if sources != record.get('sources'):
    sys.exit('Shader sources changed since verified build')
