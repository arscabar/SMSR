"""Encrypted warm-cache portability, invalidation, damage and deletion checks."""
import hashlib
import os
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import patch
from graphify_index import run

def source(path, text):
    return dict(path=path, text=text, hash=hashlib.sha256(text.encode()).hexdigest().upper())

with TemporaryDirectory(prefix='smsr-cache-test-') as folder:
    previous = os.environ['LOCALAPPDATA']
    os.environ['LOCALAPPDATA'] = folder
    try:
        files = [source('a.py', 'from b import target\ndef entry():\n return target()\n'),
                 source('b.py', 'def target():\n return 1\n')]
        request = dict(files=files, cacheId='A' * 64)
        with patch('cache_maintenance.time.time', return_value=100):
            cold = run(request)
        assert cold['cache']['misses'] == 2
        assert next(Path(folder).rglob('.last-used')).stat().st_mtime == 100
        with patch('cache_maintenance.time.time', return_value=200):
            warm = run(request)
        assert warm['cache']['hits'] == 2
        assert next(Path(folder).rglob('.last-used')).stat().st_mtime == 200
        assert all(cold[k] == warm[k] for k in ('nodes', 'edges', 'issues'))
        entries = list(Path(folder).rglob('*.bin'))
        assert len(entries) == 2 and all(b'target' not in p.read_bytes() for p in entries)
        files[1] = source('b.py', 'def other():\n return 2\n')
        changed = run(request)
        assert changed['cache']['hits'] == 1 and changed['cache']['misses'] == 1
        assert not any(n['label'] == 'target' for n in changed['nodes'])
        for item in entries:
            item.write_bytes(b'broken encrypted envelope')
        damaged = run(request)
        assert damaged['cache']['corrupt'] == 2 and damaged['cache']['misses'] == 2
        run(dict(files=files[:1], cacheId='A' * 64))
        assert len(list(Path(folder).rglob('*.bin'))) == 1
        other = run(dict(files=files[:1], cacheId='B' * 64))
        assert other['cache']['misses'] == 1
    finally:
        os.environ['LOCALAPPDATA'] = previous
print('Encrypted cache warm/change/delete/corruption/scope checks OK')
