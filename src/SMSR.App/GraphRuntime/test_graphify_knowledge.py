"""Entity/evidence/group whitelist and stable serialization regression."""
import hashlib
from pathlib import Path
from tempfile import TemporaryDirectory
from graphify_index import run
from graphify_normalize import normalize

def check():
    text = 'class Client:\n def target(self):\n  return 1\n def entry(self):\n  return self.target()\n'
    hash_value = hashlib.sha256(text.encode()).hexdigest().upper()
    result = run(dict(files=[dict(path='a.py', text=text, hash=hash_value)]))
    assert any(n['details']['entityKind'] == 'class' for n in result['nodes'])
    assert any(n['details']['entityKind'] == 'method' for n in result['nodes'])
    assert all(e['evidence']['sourceHash'] == hash_value for e in result['edges'])
    assert result == run(dict(files=[dict(path='a.py', text=text, hash=hash_value)]))
    with TemporaryDirectory() as folder:
        known = {'a.py': dict(path='a.py', hash=hash_value)}
        nodes = [dict(id=str(i), label='part' + str(i), source_file='a.py', source_location='L1-L3') for i in range(3)]
        report = dict(nodes=nodes, edges=[], hyperedges=[dict(id='auth', label='Authentication',
            nodes=['0', '1', '2'], relation='participate_in', source_file='a.py', confidence='INFERRED')])
        data = normalize(report, Path(folder), known)
        assert len(data['hyperedges']) == 1 and not data['edges']
        assert data['nodes'][0]['details']['entityKind'] == 'unknown'
        assert data['nodes'][0]['details']['endLine'] == 3
        report['nodes'][0].update(file_type='rationale', rationale='password=verysecretvalue', label='password=verysecretvalue')
        data = normalize(report, Path(folder), known)
        assert data['nodes'][0]['details']['rationale'] == '[비밀값 제외]'
        assert data['nodes'][0]['label'] == '[비밀값 제외]'
        for ids in (['0', '1'], ['0', '1', 'missing'], ['0', '1', '1']):
            report['hyperedges'][0]['nodes'] = ids
            try:
                normalize(report, Path(folder), known)
            except ValueError:
                continue
            raise AssertionError('Invalid membership accepted')
    print('Entity kinds/ranges, evidence hashes, groups, redaction and stable output OK')

if __name__ == '__main__':
    check()
