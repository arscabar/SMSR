"""Static call-site preservation without overload, receiver or local guesses."""
import hashlib
from graphify_index import run


def analyze(text):
    return run(dict(files=[dict(path='Calls.cs', text=text,
                               hash=hashlib.sha256(text.encode()).hexdigest().upper())]))


text = '''class C {
 void Target() {}
 void Go() {
  Target();
  Target();
  Unknown.Absent();
 }
}'''
facts = analyze(text)
calls = [e for e in facts['edges'] if e['relation'] == 'CALLS']
assert {e['sourceLine'] for e in calls} == {4, 5}, calls
assert any('Absent' in i['reason'] for i in facts['issues']), facts['issues']
assert not any('Absent' in n['label'] for n in facts['nodes'])
shadow = analyze(text.replace('void Go()', 'void Go(System.Action Target)'))
assert not any(e['sourceLine'] == 5 and e['relation'] == 'CALLS' for e in shadow['edges'])
escaped = analyze(text.replace('void Go()', 'void Go(System.Action @Target)'))
assert not any(e['sourceLine'] == 5 and e['relation'] == 'CALLS' for e in escaped['edges'])
overload = analyze(text.replace('void Target() {}', 'void Target() {}\n void Target(int x) {}'))
assert not any(e['sourceLine'] == 6 and e['relation'] == 'CALLS' for e in overload['edges'])
assert facts == analyze(text)
explicit = analyze(text.replace('Target();', 'this.Target();'))
assert {e['sourceLine'] for e in explicit['edges'] if e['relation']=='CALLS'} == {4,5}
assert not any('None.' in i['reason'] for i in explicit['issues'])
unbound = analyze(text.replace('Target();', 'other.Target();'))
assert not any(e['sourceLine']==5 and e['relation']=='CALLS' for e in unbound['edges'])
delegate = analyze('''class Helper { public void Target() {} }
class C {
 System.Action Target;
 void Go(Helper h) {
  h.Target();
  Target();
  Target();
 }
}''')
assert not any(e['sourceLine'] == 7 and e['relation'] == 'CALLS' for e in delegate['edges']), delegate['edges']
# Upstream's existing first ambiguous binding is not promoted with another call site.
alias = analyze('''class C {
 string s = "w000";
 void with() {}
 void Go(object[] items) { foreach (var with in items) { } }
}''')
assert any(n['label'] == '.with()' for n in alias['nodes']), alias['nodes']
assert not any('w001' in n['label'] for n in alias['nodes'])
print('Repeated sites, unknown receivers, shadowing, overloads and stable identity passed')
