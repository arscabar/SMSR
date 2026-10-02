"""Run synthetic language fixtures through unmodified installed Graphify."""
import json
import sys
import tempfile
from pathlib import Path
from graphify_grammars import install
install()
from graphify.extract import extract
from language_cases import CASES

result = {}
for language, (extension, source) in CASES.items():
    with tempfile.TemporaryDirectory() as folder:
        root = Path(folder).resolve()
        path = root / ('sample.' + extension)
        path.write_text(source, encoding='utf-8')
        report = extract([path], root=root, cache_root=root, parallel=False)
        real = [node for node in report['nodes'] if node.get('source_file') == path.name]
        real_ids = {node['id'] for node in real}
        result[language] = dict(nodes=sum(node['label'] != path.name for node in real),
            calls=sum(edge['relation'] == 'calls' and edge['source'] in real_ids and edge['target'] in real_ids
                for edge in report['edges']))
print(json.dumps(result))
