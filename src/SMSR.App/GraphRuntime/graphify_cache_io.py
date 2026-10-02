"""DPAPI AST cache callbacks, used only while holding the namespace lease."""
import hashlib
from cache_protection import protect
from cache_paths import checked, regular

def callbacks(cache, module, root, folder, stats):
    old_load, old_save = module.load_cached, module.save_cached
    seen, active = set(), set()
    def entry(path):
        key = hashlib.sha256(path.relative_to(root).as_posix().encode()).hexdigest()
        active.add(key + '.bin')
        digest = cache.file_hash(path, root, cache_root=root)
        return checked(folder / (key + '.bin')), cache.cache_dir(root) / (digest + '.json')
    def load(path, *args, **kwargs):
        persisted, temporary = entry(path)
        if not temporary.exists() and persisted.exists():
            try:
                if regular(persisted).stat().st_size > 16 * 1024 * 1024:
                    raise ValueError('Cache size')
                raw = protect(persisted.read_bytes(), True)
                digest, payload = raw.split(b'\n', 1)
                if digest.decode() == temporary.stem:
                    temporary.write_bytes(payload)
            except (OSError, ValueError):
                stats['corrupt'] += 1
        value = old_load(path, *args, **kwargs)
        if path not in seen:
            stats['hits' if value else 'misses'] += 1
            seen.add(path)
        return value
    def save(path, result, *args, **kwargs):
        old_save(path, result, *args, **kwargs)
        persisted, temporary = entry(path)
        if temporary.exists() and temporary.stat().st_size <= 16 * 1024 * 1024:
            pending = checked(persisted.with_suffix('.tmp'))
            if pending.exists():
                regular(pending)
            pending.write_bytes(protect(temporary.stem.encode() + b'\n' + temporary.read_bytes()))
            checked(persisted)
            pending.replace(persisted)
    return load, save, active
