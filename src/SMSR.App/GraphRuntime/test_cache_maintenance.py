"""Deterministic TTL/quota and active namespace preservation checks."""
import os
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import patch
from cache_paths import root
from cache_maintenance import lease, touch, trim
from cache_lock import locked

def cached(name, used, size):
    folder = root() / ('a' * 16) / (name * 32)
    folder.mkdir(parents=True)
    (folder / ('b' * 64 + '.bin')).write_bytes(b'x' * size)
    touch(folder, used)
    return folder

previous = os.environ['LOCALAPPDATA']
with TemporaryDirectory(prefix='smsr-maintenance-test-') as temporary:
    os.environ['LOCALAPPDATA'] = temporary
    try:
        now = 4_000_000
        assert trim.__kwdefaults__['quota'] == 1024 ** 3
        assert trim.__kwdefaults__['ttl'] == 30 * 86400
        old = cached('A', now - 30 * 86400, 5)
        recent = cached('B', now - 1, 7)
        result = trim(now=now)
        assert result['removedFiles'] == 1 and result['removedBytes'] == 5
        assert not list(old.glob('*.bin')) and list(recent.glob('*.bin'))
        older = cached('C', now - 2, 10)
        result = trim(now=now, quota=7)
        assert result['removedBytes'] == 10 and result['totalBytes'] == 7
        assert list(recent.glob('*.bin')) and not list(older.glob('*.bin'))
        with locked(recent / '.lock'):
            result = trim(now=now + 31 * 86400, quota=0)
            assert result['activeSkipped'] == 1 and result['overLimit']
            assert list(recent.glob('*.bin'))
        with patch('cache_maintenance.time.time', return_value=now + 3):
            with lease(recent):
                with lease(recent):
                    assert (recent / '.last-used').stat().st_mtime == now + 3
                assert list(recent.glob('*.bin'))
        assert (recent / '.last-used').stat().st_mtime == now + 3
        assert not trim(now=now + 3)['overLimit']
        print('Cache TTL/quota/active/nested last-use checks OK')
    finally:
        os.environ['LOCALAPPDATA'] = previous
