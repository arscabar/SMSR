"""Protected resolver facts; no source bodies or source-tree cache writes."""
import json
import re
import uuid
from pathlib import PurePosixPath
from cache_protection import protect

LIMIT = 64 * 1024 * 1024

def manifest(known):
    return {item['path']: item['hash'] for item in known.values()}

def identifiers(root, known):
    # Lookup keys include unresolved names and aliases, not just existing edges.
    return {item['path']: sorted(set(re.findall(r'\b[^\W\d]\w*\b',
        (root / item['path']).read_text(encoding='utf-8')))) for item in known.values()}

def valid_path(path):
    relative = PurePosixPath(path)
    return bool(path and relative.as_posix() == path and not relative.is_absolute()
        and not any(p == '..' for p in relative.parts) and '\\' not in path and ':' not in path)

def load(folder):
    path = folder / 'rawcontext.bin'
    if not path.exists():
        return None
    try:
        info = path.lstat()
        if path.is_symlink() or getattr(info, 'st_file_attributes', 0) & 0x400 or info.st_size > LIMIT:
            return None
        value = json.loads(protect(path.read_bytes(), True))
        files = value.get('manifest', {})
        if value.get('format') != 1 or not isinstance(files, dict) or len(files) > 50000:
            return None
        if any(not valid_path(p) or not re.fullmatch('[A-F0-9]{64}', h) for p, h in files.items()):
            return None
        if set(value['keys']) != set(files) or any(not isinstance(v, list) or any(not isinstance(k, str) for k in v) for v in value['keys'].values()):
            return None
        report = value['report']
        if report.get('failed_sources') or not isinstance(report['nodes'], list) or not isinstance(report['edges'], list):
            return None
        if any(n.get('source_file') not in files for n in report['nodes']):
            return None
        return value
    except (OSError, ValueError, KeyError, TypeError):
        return None

def save(folder, known, keys, report, issues):
    value = dict(format=1, manifest=manifest(known), keys=keys, report=report, issues=issues)
    raw = json.dumps(value, ensure_ascii=False, separators=(',', ':')).encode('utf-8')
    if len(raw) > LIMIT - 4096:
        return False
    pending = folder / ('rawcontext-' + uuid.uuid4().hex + '.tmp')
    try:
        pending.write_bytes(protect(raw))
        pending.replace(folder / 'rawcontext.bin')
        return True
    finally:
        pending.unlink(missing_ok=True)
