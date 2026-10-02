"""Original installed Graphify: duplicate symbols across three scoped files."""
import hashlib
import json
import tempfile
from contextlib import redirect_stdout
from pathlib import Path
import sys
from graphify_grammars import install
install()
from graphify.extract import extract
from graphify_normalize import normalize
from language_cases import CASES

def signature(facts):
    return dict(nodes=sorted((n['nodeId'], n['sourcePath'], n['line'], n['label']) for n in facts['nodes']),
        edges=sorted(tuple(e[k] for k in ('sourceId','targetId','relation','ownerPath','sourceLine','resolution','confidence')) for e in facts['edges']))

result={}
for language,(extension,source) in CASES.items():
    with tempfile.TemporaryDirectory() as folder:
        root=Path(folder).resolve()
        paths=[root/f'{prefix}/sample.{extension}' for prefix in ('a','b','c')]
        known={}
        for path in paths:
            path.parent.mkdir();path.write_text(source,encoding='utf-8')
            relative=path.relative_to(root).as_posix()
            known[relative]=dict(path=relative,hash=hashlib.sha256(source.encode()).hexdigest().upper())
        with redirect_stdout(sys.stderr):
            facts=normalize(extract(paths,root=root,cache_root=root,parallel=False),root,known)
        result[language]=signature(facts)
print(json.dumps(result))
