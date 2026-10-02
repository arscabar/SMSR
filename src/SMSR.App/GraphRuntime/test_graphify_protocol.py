"""Large upstream progress and legacy text must not corrupt worker JSON."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

text = '# 한글 원문\ndef target(x):\n return x\n'
original = hashlib.sha256(text.encode('cp949')).hexdigest().upper()
converted = hashlib.sha256(text.encode()).hexdigest().upper()
files = [dict(path=f'src/item{i}.py', text=text, hash=original, contentHash=converted)
         for i in range(100)]
result = subprocess.run([sys.executable, str(Path(__file__).with_name('worker.py'))],
    input=json.dumps(dict(operation='index-code', files=files)), capture_output=True,
    text=True, encoding='utf-8', timeout=60, check=True)
facts = json.loads(result.stdout)
assert len(facts['nodes']) == 100
assert all(n['hash'] == original for n in facts['nodes'])
assert 'AST extraction' in result.stderr
files[0]['contentHash'] = '0' * 64
rejected = subprocess.run([sys.executable, str(Path(__file__).with_name('worker.py'))],
    input=json.dumps(dict(operation='index-code', files=files)), capture_output=True,
    text=True, encoding='utf-8', timeout=60)
assert rejected.returncode != 0 and json.loads(rejected.stdout)['error']
print('100-file JSON isolation, converted content hash, bad hash rejection OK')
from graphify_index import run
broken = 'def broken(:\n pass\n'
digest = hashlib.sha256(broken.encode()).hexdigest().upper()
partial = run(dict(files=[dict(path='src/broken.py', text=broken, hash=digest)]))
assert any(i['relation'] == 'PARSE_ERROR' for i in partial['issues'])
print('Cached upstream parse warning surfaced OK')
broken_js = 'function broken( { return 1; }'
digest = hashlib.sha256(broken_js.encode()).hexdigest().upper()
partial = run(dict(files=[dict(path='src/broken.js', text=broken_js, hash=digest)]))
assert any(i['relation'] == 'PARSE_ERROR' and '심벌' in i['reason'] for i in partial['issues'])
bom = '\ufeffdef target():\n return 1\n'
digest = hashlib.sha256(bom.encode()).hexdigest().upper()
clean = run(dict(files=[dict(path='bom.py', text=bom, hash=digest)]))
assert clean['nodes'] and not any(i['relation'] == 'PARSE_ERROR' for i in clean['issues'])
assert all(n['hash'] == digest for n in clean['nodes'])
print('Uncached JavaScript recovery and BOM/original-hash checks OK')
