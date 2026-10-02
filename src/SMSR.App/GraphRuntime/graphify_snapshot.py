"""Only validated, hash-matched index inputs are visible to upstream resolvers."""
import hashlib
import re
from pathlib import Path, PurePosixPath

def create(root, files):
    if not isinstance(files, list) or len(files) > 50000:
        raise ValueError('Invalid file count')
    known, total = {}, 0
    for item in files:
        path = item['path']
        relative = PurePosixPath(path)
        if not path or len(path) > 1024 or relative.as_posix() != path or relative.is_absolute() or any(p in ('..', '') for p in relative.parts) or '\\' in path or ':' in path:
            raise ValueError('Invalid relative path')
        if path.casefold() in known:
            raise ValueError('Duplicate path')
        raw = item['text'].encode('utf-8')
        total += len(raw)
        if len(raw) > 2000000 or total > 256 * 1024 * 1024 or b'\0' in raw:
            raise ValueError('Source size/binary limit')
        digest = hashlib.sha256(raw).hexdigest().upper()
        if not re.fullmatch('[0-9A-F]{64}', item['hash']) or digest != item.get('contentHash', item['hash']):
            raise ValueError('Source hash mismatch')
        target = root.joinpath(*relative.parts)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(raw.removeprefix(b'\xef\xbb\xbf'))
        known[path.casefold()] = dict(path=path, hash=item['hash'])
    return known

def relative_source(value, root, known):
    try:
        path = Path(value)
        relative = path.resolve().relative_to(root.resolve()).as_posix() if path.is_absolute() else path.as_posix()
        return known.get(relative.casefold())
    except (ValueError, OSError, TypeError):
        return None
