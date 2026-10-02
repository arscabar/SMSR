"""Over-32-MiB aggregate reaches whole-scope resolution via encrypted batches."""
import hashlib
import json
from pathlib import Path
from tempfile import TemporaryDirectory
from cache_protection import protect
from graphify_batches import files
from graphify_index import run

with TemporaryDirectory(prefix='smsr-code-batch-') as folder:
    root = Path(folder)
    source = '#' + 'x' * 1400000 + '\ndef target():\n return 1\n'
    digest = hashlib.sha256(source.encode()).hexdigest().upper()
    for batch in range(3):
        items = [dict(path=f'item{batch * 8 + i}.py', text=source, hash=digest) for i in range(8)]
        (root / f'{batch}.bin').write_bytes(protect(json.dumps(items).encode()))
    request = dict(batchFolder=folder, batchCount=3)
    result = run(request)
    assert len(result['nodes']) == 24
    for bad in (dict(batchFolder=str(root.parent), batchCount=3),
                dict(batchFolder=folder, batchCount=6251)):
        try:
            files(bad)
            raise AssertionError('Unsafe batch accepted')
        except ValueError:
            pass
    (root / '0.bin').write_bytes(b'corrupt')
    try:
        files(request)
        raise AssertionError('Unprotected batch accepted')
    except OSError:
        pass
print('33-MiB corpus, encrypted batches, aggregate/scope/damage checks OK')
