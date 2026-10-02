"""Offline checks of the pinned comparison and unchanged runtime boundary."""
import ast
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / 'src/SMSR.App/GraphRuntime'

def check():
    baseline = json.loads((ROOT / 'docs/test-data/graphify-baseline-2026-10-01.json').read_text(encoding='utf-8'))
    upstream = json.loads((RUNTIME / 'vendor/UPSTREAM.json').read_text(encoding='utf-8'))
    assert baseline['runtimeVersion'] == upstream['version']
    assert baseline['runtimeRevision'] == upstream['revision']
    assert baseline['comparisonRevision'] == 'ef4450d9c28acb2b8cdc22d369c1777b77148eef'
    assert baseline['classificationIsNotRelationshipSupport']
    for module, digest in upstream['modules'].items():
        path = RUNTIME / 'vendor' / (module.replace('.', '/') + '.py')
        if not path.exists():
            path = RUNTIME / 'vendor' / module.replace('.', '/') / '__init__.py'
        assert hashlib.sha256(path.read_bytes()).hexdigest() == digest, module
    tree = ast.parse((RUNTIME / 'symbols.py').read_text(encoding='utf-8'))
    languages = next(ast.literal_eval(n.value) for n in tree.body if isinstance(n, ast.Assign)
                     and isinstance(n.targets[0], ast.Name) and n.targets[0].id == 'LANGUAGES')
    assert baseline['runtimeAstExtensions'] == languages
    for name in ('LICENSE', 'LICENSE-MIT', 'NOTICE'):
        assert (RUNTIME / 'vendor' / name).stat().st_size > 0
    fixture = json.loads((ROOT / 'docs/test-data/graphify-knowledge-fixtures.json').read_text(encoding='utf-8'))
    assert {'code', 'document', 'image', 'audio', 'video', 'office', 'negative'} <= set(fixture)
    assert all(fixture[k] for k in fixture)
    print(f'PASS: pinned baseline, {len(upstream["modules"])} original modules, runtime allowlist, licenses and fixture categories')

if __name__ == '__main__':
    check()
