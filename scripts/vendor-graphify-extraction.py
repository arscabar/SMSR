"""Vendor a verified checkout's extraction closure, never its CLI or server."""
import ast
import hashlib
import json
import subprocess
import sys
import shutil
import importlib.util
import tomllib
from pathlib import Path

REVISION = 'ef4450d9c28acb2b8cdc22d369c1777b77148eef'
VERSION = '0.9.73'

def main(checkout):
    root = Path(checkout).resolve()
    commit = subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD'], text=True).strip()
    assert commit == REVISION, 'Wrong upstream commit'
    assert tomllib.loads((root / 'pyproject.toml').read_text())['project']['version'] == VERSION
    source = root / 'graphify'
    target = Path(__file__).resolve().parents[1] / 'src/SMSR.App/GraphRuntime/vendor'
    pending, selected = ['graphify.extract', 'graphify.cluster', 'graphify.analyze'], {}
    while pending:
        module = pending.pop()
        if module in selected:
            continue
        relative = Path(*module.split('.')[1:])
        path, package = source / relative.with_suffix('.py'), module.rsplit('.', 1)[0]
        if not path.is_file():
            path, package = source / relative / '__init__.py', module
        raw = path.read_bytes()
        selected[module] = (path, raw)
        for node in ast.walk(ast.parse(raw)):
            names = []
            if isinstance(node, ast.Import):
                names = [item.name for item in node.names]
            elif isinstance(node, ast.ImportFrom):
                name = node.module or ''
                if node.level:
                    name = importlib.util.resolve_name('.' * node.level + name, package)
                names = [name]
            pending.extend(n for n in names if n.startswith('graphify.') and n not in selected)
    # Resolve all modules before any mechanical vendor replacement.
    for path, raw in selected.values():
        destination = target / 'graphify' / path.relative_to(source)
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(path, destination)
        for ancestor in destination.parents:
            if ancestor == target:
                break
            original = source / ancestor.relative_to(target / 'graphify') / '__init__.py'
            if original.is_file():
                shutil.copyfile(original, ancestor / '__init__.py')
    for name in ('LICENSE', 'LICENSE-MIT', 'NOTICE'):
        shutil.copyfile(root / name, target / name)
    manifest = dict(version=VERSION, revision=REVISION,
        source='https://github.com/Graphify-Labs/graphify',
        modules={m: hashlib.sha256(raw).hexdigest() for m, (_, raw) in sorted(selected.items())})
    (target / 'UPSTREAM.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print('Vendored verified extraction modules:', len(selected))

if __name__ == '__main__':
    main(sys.argv[1])
