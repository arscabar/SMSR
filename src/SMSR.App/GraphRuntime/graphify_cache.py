"""Persist only upstream AST cache envelopes, encrypted outside the source tree."""
import hashlib
import re
from contextlib import contextmanager
from importlib.metadata import version
from pathlib import Path
from cache_paths import root as cache_root, regular
from cache_maintenance import lease
from graphify_cache_io import callbacks
from cache_inventory import erase

def namespace(cache_id):
    if not re.fullmatch('[A-F0-9]{64}', cache_id):
        raise ValueError('Invalid cache namespace')
    base = Path(__file__).parent
    stamp = (base / 'vendor/UPSTREAM.json').read_bytes()
    adapters = [p for p in base.glob('*.py') if not p.name.startswith('test_')]
    for path in sorted(adapters):
        stamp += path.name.encode() + b'\0' + path.read_bytes() + b'\0'
    stamp += (version('tree-sitter') + version('tree-sitter-language-pack')).encode()
    return cache_root() / hashlib.sha256(stamp).hexdigest()[:16] / cache_id[:32]

@contextmanager
def reuse(module, root, cache_id, retained_paths=None, namespace_folder=None):
    stats = dict(hits=0, misses=0, corrupt=0)
    if not cache_id:
        yield stats
        return
    from graphify import cache
    folder = namespace_folder if namespace_folder is not None else namespace(cache_id)
    if folder != namespace(cache_id):
        raise ValueError('Cache adapter changed before extraction')
    with lease(folder) as maintenance:
        old_load, old_save = module.load_cached, module.save_cached
        load, save, active = callbacks(cache, module, root, folder, stats)
        for value in retained_paths or []:
            path = Path(value)
            relative = path.relative_to(root) if path.is_absolute() else path
            if '..' in relative.parts:
                raise ValueError('Invalid retained cache path')
            active.add(hashlib.sha256(relative.as_posix().encode()).hexdigest() + '.bin')
        module.load_cached, module.save_cached = load, save
        try:
            yield stats
            if folder != namespace(cache_id):
                erase(folder)
                raise ValueError('Cache adapter changed during extraction')
            for item in folder.glob('*.bin'):
                if re.fullmatch(r'[a-f0-9]{64}\.bin', item.name) and item.name not in active:
                    regular(item).unlink()
        finally:
            module.load_cached, module.save_cached = old_load, old_save
    stats['maintenance'] = maintenance
