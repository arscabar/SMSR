"""Conditional attributes must not erase the containing type or invent executable branches."""
import hashlib
from graphify_index import run

def source(path, text):
    return dict(path=path, text=text, hash=hashlib.sha256(text.encode()).hexdigest().upper())

target = '''namespace Demo {
public class Instruction {
#if DEBUG
[System.ComponentModel.Browsable(true)]
#else
[System.ComponentModel.Browsable(false)]
#endif
public string Name { get; set; }
public virtual void ResetIResult() {}
}}
'''
caller = 'using Demo; class Entry { void Go(Instruction inst) { inst.ResetIResult(); } }'
facts = run(dict(files=[source('Instruction.cs', target), source('Entry.cs', caller)]))
assert any(n['label'] == 'Instruction' for n in facts['nodes'])
assert any(e['relation'] == 'METHOD' and e['ownerPath'] == 'Instruction.cs' for e in facts['edges'])
assert any(e['relation'] == 'CALLS' and e['ownerPath'] == 'Entry.cs' for e in facts['edges'])
unknown = run(dict(files=[source('Unknown.cs', 'class Entry { void Go(object worker) { worker.Absent(); } }')]))
assert not any(e['relation'] == 'CALLS' for e in unknown['edges'])
assert any(i['relation'] == 'CALLS' and 'worker.Absent' in i['reason'] for i in unknown['issues'])
assert all('text' not in i for i in unknown['issues'])
from graphify_csharp_recovery import recover
from types import SimpleNamespace
from pathlib import Path
from tempfile import TemporaryDirectory
with TemporaryDirectory() as folder:
    path = Path(folder) / 'bad.cs'
    path.write_text('class C {\n#if DEBUG\nvoid A() {}\n#else\nvoid B() {}\n#endif\n}', encoding='utf-8')
    original = dict(parse_errors=dict(first_error_line=1), nodes=[])
    def forbidden(*args, **kwargs): raise AssertionError('Executable branch normalized')
    assert recover(SimpleNamespace(_extract_generic=forbidden), path, original) is original
print('Conditional attributes/typed cross-file call recovered; executable branches untouched')
