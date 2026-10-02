"""Full/partial normalized oracles for scoped changes and newly resolved calls."""
import hashlib
import os
from pathlib import Path
from tempfile import TemporaryDirectory
from graphify_index import run
from graphify_cache import namespace
from graphify_incremental_context import load

def source(path, text):
    return dict(path=path, text=text, hash=hashlib.sha256(text.encode()).hexdigest().upper())

def facts(value):
    return {k: sorted(value[k], key=str) for k in ('nodes', 'edges', 'issues')}

def update(files, previous, cache='D' * 64, partial=True):
    value = run(dict(files=files, cacheId=cache, previousManifest={f['path']: f['hash'] for f in previous}))
    expected = run(dict(files=files))
    if facts(value) != facts(expected):
        for key in ('nodes', 'edges', 'issues'):
            missing = [x for x in expected[key] if x not in value[key]]
            extra = [x for x in value[key] if x not in expected[key]]
            if missing or extra:
                raise AssertionError((key, missing[:3], extra[:3], value['analysis']))
    if partial:
        assert value['analysis']['mode'] == 'INCREMENTAL', value['analysis']
        assert value['analysis']['reusedFiles'] > 0
    return value

def check():
    previous_local = os.environ['LOCALAPPDATA']
    with TemporaryDirectory(prefix='smsr-incremental-test-') as temporary:
        os.environ['LOCALAPPDATA'] = temporary
        try:
            files = [source('a.py', 'from b import target\ndef entry():\n return target()\n'),
                source('b.py', 'def target():\n return 1\n'), source('alone.py', 'def alone():\n return 9\n')]
            cold = run(dict(files=files, cacheId='D' * 64, previousManifest={}))
            state = load(namespace('D' * 64))
            assert state and state['manifest'] == {f['path']: f['hash'] for f in files}
            assert all(n.get('_callable') for n in state['report']['nodes'] if n['label'] == 'target()')
            assert facts(cold) == facts(run(dict(files=files)))
            print('C1 encrypted raw context and normalized full oracle OK')
            old = files[:]
            files[0] = source('a.py', 'from b import target\ndef entry():\n return target() + 2\n')
            assert update(files, old)['reparsedPaths'] == ['a.py']
            old = files[:]
            files[1] = source('b.py', 'def renamed():\n return 1\n')
            update(files, old)
            old = files[:]
            files.append(source('new.py', 'def target():\n return 4\n'))
            update(files, old)
            old = files[:]
            files = [f for f in files if f['path'] != 'new.py']
            deletion = update(files, old, partial=False)
            assert deletion['analysis']['mode'] == 'FULL' and deletion['analysis']['fallbackReason']
            print('Python change/rename/new unresolved-name/deletion full equivalence OK')
        finally:
            os.environ['LOCALAPPDATA'] = previous_local

if __name__ == '__main__':
    check()
