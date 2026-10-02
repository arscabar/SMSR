"""Offline scanner/dispatcher parity and dependency grades for the pinned runtime."""
import ast
import importlib.util
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'src/SMSR.App/GraphRuntime'
sys.path.insert(0, str(RUNTIME))
from graphify_support import DOCUMENTS, FILE_ONLY, OPTIONAL, VERSION

def check():
    tree = ast.parse((RUNTIME / 'vendor/graphify/extract.py').read_text(encoding='utf-8'))
    dispatch = next(n.value for n in tree.body if isinstance(n, ast.AnnAssign)
                    and getattr(n.target, 'id', None) == '_DISPATCH')
    extractors = {key.value.lower(): value.id for key, value in zip(dispatch.keys, dispatch.values)}
    extensions = set(extractors) - DOCUMENTS - {'.dmi'}
    code = (ROOT / 'src/SMSR.App/Mvp/GraphCodeFormats.cs').read_text(encoding='utf-8').split('FileOnly')[0]
    assert extensions == set(re.findall(r'"(\.[a-z0-9]+)"', code)), 'Scanner/dispatch drift'
    manifest = json.loads((RUNTIME / 'vendor/UPSTREAM.json').read_text(encoding='utf-8'))
    assert manifest['version'] == VERSION
    rows = []
    for ext in sorted(extensions):
        module = OPTIONAL.get(ext)
        dependency = FILE_ONLY.get(ext) or (module if module and importlib.util.find_spec(module) is None else None)
        rows.append(dict(extension=ext, extractor=extractors[ext],
                         status='FILE_ONLY' if dependency else 'EXTRACTOR_AVAILABLE', missing=dependency))
    # Availability isn't an accuracy claim; execution oracles are reported separately.
    return dict(version=VERSION, revision=manifest['revision'], extensions=rows,
                availabilityIsNotAccuracy=True)

if __name__ == '__main__':
    print(json.dumps(check(), ensure_ascii=False))
