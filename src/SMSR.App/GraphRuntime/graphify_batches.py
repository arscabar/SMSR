"""Bounded current-user encrypted manifests from the trusted .NET indexer."""
import json
import tempfile
from pathlib import Path
from cache_protection import protect

def files(request):
    if 'batchFolder' not in request:
        return request['files']
    root = Path(request['batchFolder'])
    count = request['batchCount']
    if (not root.is_absolute() or root.parent.resolve() != Path(tempfile.gettempdir()).resolve()
            or not root.name.startswith('smsr-code-batch-') or root.is_symlink()
            or type(count) is not int or not 1 <= count <= 6250):
        raise ValueError('Invalid batch root/count')
    result, total = [], 0
    for index in range(count):
        path = root / f'{index}.bin'
        if path.is_symlink() or path.stat().st_size > 33 * 1024 * 1024:
            raise ValueError('Invalid batch size/link')
        raw = protect(path.read_bytes(), True)
        total += len(raw)
        if len(raw) > 32 * 1024 * 1024 or total > 512 * 1024 * 1024:
            raise ValueError('Batch aggregate limit')
        items = json.loads(raw)
        if not isinstance(items, list) or len(items) > 8:
            raise ValueError('Batch item count')
        result.extend(items)
    return result
