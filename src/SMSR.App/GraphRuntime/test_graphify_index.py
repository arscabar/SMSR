"""Sixteen grammar oracles plus identity, ambiguity and trust-boundary checks."""
import hashlib
import json
import subprocess
import sys
from pathlib import Path
from tempfile import TemporaryDirectory
from graphify_index import run
from graphify_snapshot import create
from graphify_normalize import normalize
from language_cases import CASES

def file(path, text):
    return dict(path=path, text=text, hash=hashlib.sha256(text.encode()).hexdigest().upper())

def check():
    oracle = None
    if len(sys.argv) == 2:
        raw = subprocess.run([sys.argv[1], str(Path(__file__).with_name('graphify_upstream_oracle.py'))],
            capture_output=True, text=True, encoding='utf-8', check=True, timeout=60)
        oracle = json.loads(raw.stdout)
    for language, (extension, source) in CASES.items():
        request = dict(files=[file('src/sample.' + extension, source)])
        result = run(request)
        assert any(n['label'].lstrip('.').startswith('target') for n in result['nodes']), language
        assert any(e['relation'] == 'CALLS' for e in result['edges']), language
        assert result == run(request), 'Non-deterministic identities: ' + language
        if oracle:
            assert oracle[language] == dict(nodes=len(result['nodes']),
                calls=sum(e['relation'] == 'CALLS' for e in result['edges'])), 'Upstream mismatch: ' + language
        print(language + ': declarations/calls/stable identity OK')
    result = run(dict(files=[file('src/a.py', 'from .b import target\ndef entry():\n return target(1)\n'),
        file('src/b.py', 'def target(x):\n return x\n')]))
    assert any(e['relation'] == 'CALLS' for e in result['edges']), 'Cross-file call missing'
    assert all('text' not in n for n in result['nodes']), 'Source body persisted'
    with TemporaryDirectory() as folder:
        root = Path(folder)
        for bad in ('../outside.py', '/outside.py', 'C:/outside.py', 'a\\b.py'):
            try:
                create(root, [file(bad, 'pass')])
            except ValueError:
                continue
            raise AssertionError('Unsafe path accepted')
        report = dict(nodes=[], edges=[dict(source='absent', target='other', relation='calls',
            source_file='src/a.py', source_location='L1', confidence='AMBIGUOUS')])
        normalized = normalize(report, root, {'src/a.py': dict(path='src/a.py', hash='A')})
        assert not normalized['edges'] and normalized['issues'], 'Ambiguity manufactured edge'
    print('Cross-file, stable identities, ambiguity and source boundaries OK')

if __name__ == '__main__':
    check()
