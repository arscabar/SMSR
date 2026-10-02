"""More than ten thousand files resolve together, not as disconnected batch graphs."""
import hashlib
import time
from graphify_index import run
from graphify_snapshot import create
from pathlib import Path
from tempfile import TemporaryDirectory

def check():
    files = []
    for i in range(10001):
        text = f'def item_{i}():\n return {i}\n'
        if i == 10000:
            text = 'from .f00000 import item_0\n' + text + '\ndef entry():\n return item_0()\n'
        files.append(dict(path=f'src/f{i:05}.py', text=text,
                          hash=hashlib.sha256(text.encode()).hexdigest().upper()))
    start = time.monotonic()
    facts = run(dict(files=files))
    assert len(facts['nodes']) == 10002, len(facts['nodes'])
    ids = {n['nodeId']: n for n in facts['nodes']}
    assert any(e['relation'] == 'CALLS' and ids[e['targetId']]['ownerPath'] == 'src/f00000.py'
               for e in facts['edges']), 'Cross-batch call lost'
    with TemporaryDirectory() as folder:
        try:
            create(Path(folder), [files[0]] * 50001)
            raise AssertionError('Unbounded file count accepted')
        except ValueError:
            pass
    print(f'10001 files / {len(facts["nodes"])} symbols / cross-file call / cap rejection: {time.monotonic()-start:.2f}s')

if __name__ == '__main__':
    check()
