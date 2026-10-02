"""Exact declaration kinds, ranges and owner retention without stub guessing."""
import hashlib
import os
from tempfile import TemporaryDirectory
from unittest.mock import patch
from graphify_index import run

text = '''namespace N {
interface I { void Go(); }
struct S { public int P {get;set;} }
enum E { One, Two }
class C {
 void Go() {
  System.Console.WriteLine("test");
 }
} }
'''
data = run(dict(files=[dict(path='Kinds.cs', text=text,
    hash=hashlib.sha256(text.encode()).hexdigest().upper())]))
def kind(label):
    return [n['details']['entityKind'] for n in data['nodes'] if n['label']==label]
assert kind('I') == ['interface'] and kind('S') == ['struct']
assert kind('E') == ['enum'] and kind('One') == ['enum_member']
assert kind('P') == ['property']
method = next(n for n in data['nodes'] if n['label']=='.Go()' and n['line']==6)
assert method['details']['endLine']==8 and method['details']['sourceLocation']=='L6'
owner = next(n for n in data['nodes'] if n['label']=='C')
assert method['details']['ownerNodeId']==owner['nodeId']
assert not any(n['label']=='WriteLine()' for n in data['nodes'])
with TemporaryDirectory() as folder, patch.dict(os.environ, LOCALAPPDATA=folder):
    request=dict(cacheId='D'*64,previousManifest={},files=[dict(path='Kinds.cs',text=text,
        hash=hashlib.sha256(text.encode()).hexdigest().upper())])
    first=run(request)
    second=run(request)
    assert first['nodes']==second['nodes'], 'Cached AST lost declaration metadata'
print('PASS exact C# declaration kinds, ranges and method owners')
