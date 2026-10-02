"""Resolver metadata stays encrypted and tied to a validated manifest."""
from pathlib import Path
from tempfile import TemporaryDirectory
from graphify_incremental_context import load, save

def check():
    with TemporaryDirectory() as temporary:
        folder = Path(temporary)
        known = {'src/a.py': dict(path='src/a.py', hash='A' * 64)}
        report = dict(nodes=[dict(id='a_target', source_file='src/a.py', label='target')], edges=[])
        assert save(folder, known, {'src/a.py': ['target']}, report, [])
        raw = (folder / 'rawcontext.bin').read_bytes()
        assert b'target' not in raw and b'src/a.py' not in raw
        value = load(folder)
        assert value['report'] == report and value['manifest'] == {'src/a.py': 'A' * 64}
        assert save(folder, {'x': dict(path='../outside.py', hash='A' * 64)}, {'../outside.py': []}, report, [])
        assert load(folder) is None, 'Out-of-bound context accepted'
        (folder / 'rawcontext.bin').write_bytes(b'corrupt')
        assert load(folder) is None, 'Corrupt context replayed'
    print('Protected manifest, lookup keys and corrupt-context fallback OK')

if __name__ == '__main__':
    check()
