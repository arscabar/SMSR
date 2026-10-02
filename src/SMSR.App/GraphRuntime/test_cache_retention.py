"""Partial AST extraction retains the full scope and raw resolver context."""
import hashlib
import os
import sys
from pathlib import Path
from tempfile import TemporaryDirectory
from types import SimpleNamespace
from unittest.mock import patch
from graphify_cache import namespace, reuse

sys.path.insert(0, str(Path(__file__).parent / 'vendor'))
module = SimpleNamespace(load_cached=lambda *a, **k: None, save_cached=lambda *a, **k: None)
previous = os.environ['LOCALAPPDATA']
with TemporaryDirectory(prefix='smsr-retention-test-') as temporary:
    os.environ['LOCALAPPDATA'] = temporary
    try:
        folder = namespace('A' * 64)
        folder.mkdir(parents=True)
        ast = lambda path: folder / (hashlib.sha256(path.encode()).hexdigest() + '.bin')
        ast('a.py').write_bytes(b'encrypted-A')
        ast('b.py').write_bytes(b'encrypted-B')
        raw = folder / 'rawcontext.bin'
        raw.write_bytes(b'encrypted-context')
        source_root = Path(temporary) / 'source'
        with reuse(module, source_root, 'A' * 64, ['a.py', 'b.py'], folder):
            pass
        assert ast('a.py').exists() and ast('b.py').exists() and raw.exists()
        with reuse(module, source_root, 'A' * 64, ['a.py'], folder):
            pass
        assert ast('a.py').exists() and not ast('b.py').exists() and raw.exists()
        with patch('graphify_cache.namespace', return_value=folder / 'other'):
            try:
                with reuse(module, source_root, 'A' * 64, ['a.py'], folder):
                    raise AssertionError('Changed adapter entered extraction')
            except ValueError:
                pass
        # Mutate only the mocked namespace while the context exits.
        manager = reuse(module, source_root, 'A' * 64, ['a.py'], folder)
        manager.__enter__()
        with patch('graphify_cache.namespace', return_value=folder / 'other'):
            try:
                manager.__exit__(None, None, None)
                raise AssertionError('Changed adapter extraction committed')
            except ValueError:
                pass
        assert not ast('a.py').exists() and not raw.exists()
        print('Partial AST/raw context retention and adapter change checks OK')
    finally:
        os.environ['LOCALAPPDATA'] = previous
