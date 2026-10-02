"""Unmodified fixed-checkout extractor oracle; only grammar loading is adapted."""
import hashlib
import json
from pathlib import Path
import tempfile
from graphify_grammars import install
install()
from graphify.extract import extract
from graphify_normalize import normalize
from language_extension_cases import CASES

result = {}
for extension, source in CASES.items():
    with tempfile.TemporaryDirectory() as folder:
        root = Path(folder).resolve()
        path = root / ('sample' + extension)
        path.write_text(source, encoding='utf-8')
        known = {path.name: dict(path=path.name, hash=hashlib.sha256(source.encode()).hexdigest().upper())}
        facts = normalize(extract([path], root=root, cache_root=root, parallel=False), root, known)
        result[extension] = dict(nodes=[n['nodeId'] for n in facts['nodes']],
            edges=[[e[k] for k in ('sourceId','targetId','relation')] for e in facts['edges']])
print(json.dumps(result))
