"""Real child-process lock contention prevents TTL/quota deletion."""
import os
import subprocess
import sys
from pathlib import Path
from tempfile import TemporaryDirectory
from cache_lock import locked
from cache_paths import root
from cache_maintenance import touch, trim

if len(sys.argv) > 1:
    with locked(Path(sys.argv[1]) / '.lock'):
        print('READY', flush=True)
        sys.stdin.readline()
    raise SystemExit

previous = os.environ['LOCALAPPDATA']
with TemporaryDirectory(prefix='smsr-lock-test-') as temporary:
    os.environ['LOCALAPPDATA'] = temporary
    child = None
    try:
        folder = root() / ('a' * 16) / ('B' * 32)
        folder.mkdir(parents=True)
        payload = folder / 'rawcontext.bin'
        payload.write_bytes(b'encrypted-context')
        now = 4_000_000
        touch(folder, now - 40 * 86400)
        child = subprocess.Popen([sys.executable, __file__, str(folder)], stdin=subprocess.PIPE,
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        assert child.stdout.readline().strip() == 'READY'
        with locked(folder / '.lock', wait=False) as acquired:
            assert not acquired
        result = trim(now=now, quota=0)
        assert result['activeSkipped'] == 1 and result['overLimit'] and payload.exists()
        child.communicate('release\n', timeout=10)
        assert child.returncode == 0
        result = trim(now=now, quota=0)
        assert result['removedFiles'] == 1 and not payload.exists()
        print('Process-shared active cache contention checks OK')
    finally:
        if child is not None and child.poll() is None:
            child.kill()
            child.communicate(timeout=10)
        os.environ['LOCALAPPDATA'] = previous
