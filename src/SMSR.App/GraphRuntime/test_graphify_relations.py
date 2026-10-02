"""Cross-file declared types and receiver calls use original Graphify rules."""
import hashlib
from graphify_index import run

def source(path, text):
    return dict(path=path, text=text, hash=hashlib.sha256(text.encode()).hexdigest().upper())

facts = run(dict(files=[source('Base.cs', 'namespace Sample; public class Base { public void Run() {} }'),
    source('Child.cs', 'namespace Sample; public class Child : Base { public void Go() { Base worker = new Base(); worker.Run(); } }')]))
assert any(e['relation'] == 'INHERITS' for e in facts['edges'])
assert any(e['relation'] == 'CALLS' and e['ownerPath'] == 'Child.cs' for e in facts['edges'])
assert any(e['relation'] in ('DEFINES', 'METHOD') for e in facts['edges'])
facts = run(dict(files=[source('a.py', 'from b import target as alias\ndef entry(x):\n return alias(x)\n'),
    source('b.py', 'def target(x):\n return x\n')]))
assert any(e['relation'] == 'CALLS' and e['ownerPath'] == 'a.py' for e in facts['edges'])
print('C# inheritance/typed receiver and Python imported alias calls OK')
