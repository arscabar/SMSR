"""A real Windows junction cannot redirect cache eviction into source data."""
import os
import subprocess
from pathlib import Path
from tempfile import TemporaryDirectory
from cache_paths import root
from cache_maintenance import trim

assert os.name == 'nt', 'Windows junction check requires Windows'
previous = os.environ['LOCALAPPDATA']
with TemporaryDirectory(prefix='smsr-junction-test-') as temporary:
    os.environ['LOCALAPPDATA'] = temporary
    junction = None
    try:
        source = Path(temporary) / 'source-repository'
        source.mkdir()
        original = source / 'rawcontext.bin'
        original.write_bytes(b'original-source-not-cache')
        version = root() / ('c' * 16)
        version.mkdir(parents=True)
        junction = version / ('D' * 32)
        result = subprocess.run(['cmd', '/c', 'mklink', '/J', str(junction), str(source)],
            capture_output=True, timeout=10, creationflags=subprocess.CREATE_NO_WINDOW)
        assert result.returncode == 0, 'Could not create isolated junction fixture'
        assert junction.lstat().st_file_attributes & 0x400
        report = trim(now=4_000_000, quota=0)
        assert report['unsafeSkipped'] == 1 and report['removedFiles'] == 0
        assert original.read_bytes() == b'original-source-not-cache'
        print('Real Windows cache junction source preservation check OK')
    finally:
        if junction is not None and junction.exists():
            os.rmdir(junction)  # Only the test-created junction, never its target.
        os.environ['LOCALAPPDATA'] = previous
