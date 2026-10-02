"""Bounded-depth inventory; unexpected entries are never deletion targets."""
import re
from cache_paths import checked, regular

DATA = re.compile(r'(?:[a-f0-9]{64}|rawcontext)\.(?:bin|tmp)$')

def inventory(base):
    rows, unsafe = [], 0
    for version in base.iterdir():
        if not re.fullmatch('[a-f0-9]{16}', version.name):
            continue
        try:
            checked(version)
            for folder in version.iterdir():
                if not re.fullmatch('[A-Fa-f0-9]{32}', folder.name):
                    continue
                try:
                    checked(folder)
                    files = list(folder.iterdir())
                    if any(not DATA.fullmatch(p.name) and p.name not in ('.lock', '.last-used') for p in files):
                        raise ValueError('Unexpected cache entry')
                    for item in files:
                        regular(item)
                    data = [p for p in files if DATA.fullmatch(p.name)]
                    size = sum(p.stat().st_size for p in data)
                    marker = folder / '.last-used'
                    used = marker.stat().st_mtime if marker.exists() else max(
                        [folder.stat().st_mtime, *(p.stat().st_mtime for p in data)])
                    rows.append((used, folder, size))
                except (OSError, ValueError):
                    unsafe += 1
        except (OSError, ValueError):
            unsafe += 1
    return sorted(rows), unsafe

def erase(folder):
    # Keep lock/marker and empty directories: unlinking a locked file permits races.
    checked(folder)
    items = list(folder.iterdir())
    for item in items:
        regular(item)
    count = size = 0
    for item in items:
        if DATA.fullmatch(item.name):
            value = regular(item)
            length = value.stat().st_size
            value.unlink()
            count += 1
            size += length
    return count, size
