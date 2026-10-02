"""Outside targets, Windows reparse paths and linked payloads are refused."""
import os
from pathlib import Path
from tempfile import TemporaryDirectory
from types import SimpleNamespace
from unittest.mock import patch
from cache_paths import checked, regular, root
from cache_maintenance import touch, trim

def refuses(path):
    try:
        checked(path)
        raise AssertionError('Unsafe cache target accepted')
    except ValueError:
        pass

previous = os.environ['LOCALAPPDATA']
with TemporaryDirectory(prefix='smsr-path-test-') as temporary:
    os.environ['LOCALAPPDATA'] = temporary
    try:
        outside = Path(temporary) / 'original-source.bin'
        outside.write_bytes(b'original-source')
        refuses(outside)
        refuses(root() / '..' / 'original-source.bin')
        folder = root() / ('a' * 16) / ('B' * 32)
        folder.mkdir(parents=True)
        touch(folder, 1)
        target = folder / 'rawcontext.bin'
        target.write_bytes(b'cache-only')
        actual = Path.lstat
        def reparse(path, *args, **kwargs):
            value = actual(path, *args, **kwargs)
            if path == target or path == folder:
                return SimpleNamespace(st_mode=value.st_mode, st_file_attributes=0x400)
            return value
        with patch.object(Path, 'lstat', reparse):
            refuses(target)
            assert trim(now=4_000_000, quota=0)['unsafeSkipped'] == 1
        target.unlink()
        try:
            target.symlink_to(outside)
        except OSError:
            pass  # Real reparse attribute refusal above is mandatory on Windows.
        else:
            refuses(target)
            assert trim(now=4_000_000, quota=0)['unsafeSkipped'] == 1
            assert outside.read_bytes() == b'original-source'
            target.unlink()
        assert outside.read_bytes() == b'original-source'
        os.link(outside, target)
        try:
            regular(target)
            raise AssertionError('Hard-linked cache accepted')
        except ValueError:
            pass
        assert trim(now=4_000_000, quota=0)['unsafeSkipped'] == 1
        assert outside.read_bytes() == b'original-source'
        target.unlink()
        print('Outside/reparse/linked cache target preservation checks OK')
    finally:
        os.environ['LOCALAPPDATA'] = previous
