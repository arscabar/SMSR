"""Declaration/call preservation across equivalent syntax and Windows annotations."""
import hashlib
from graphify_index import run
from graphify_csharp_syntax import normalize


def source(path, text):
    return dict(path=path, text=text, hash=hashlib.sha256(text.encode()).hexdigest().upper())


cs = '''unsafe class C {
 void Target() {} int Read(int* p, int offset) { Target(); return *(p + offset); }
 void Each() { foreach (var with in items) { with.Go(); Target(); } }
}'''
facts = run(dict(files=[source('Pointer.cs', cs)]))
assert not any(i['relation'] == 'PARSE_ERROR' for i in facts['issues']), facts['issues']
assert any(n['label'] == 'C' for n in facts['nodes'])
assert any(e['relation'] == 'CALLS' for e in facts['edges'])
assert not any('w000' in i['reason'] for i in facts['issues'])
assert {kind for _, kind in normalize(cs.encode())[1]} == {'pointer-index', 'contextual-identifier'}
literal = b'unsafe class C { int* p; string s="*(p + x) with"; /* *(p + x) */ }'
assert normalize(literal)[0] == literal
for expression in ('*\n (p + offset)', '*(\n p + offset)'):
    multiline = ('unsafe class C { int Read(int* p, int offset) { return ' + expression + '; } }').encode()
    assert normalize(multiline)[0] == multiline
    analyze_multiline = run(dict(files=[source('Multiline.cs', multiline.decode())]))
    assert analyze_multiline['nodes'], 'Multiline recovery broke indexing'
cast = 'unsafe class C { byte Read(byte* data, int index) { return *((byte*)((ulong)((byte*)data + index) ^ 3)); } }'
cast_facts = run(dict(files=[source('Cast.cs', cast)]))
assert not any(i['relation'] == 'PARSE_ERROR' for i in cast_facts['issues']), cast_facts['issues']
assert any(i['relation'] == 'SYNTAX_RECOVERY' for i in cast_facts['issues'])
cpp = '''class Win { public:
 IFACEMETHODIMP Advise(__in Events* events);
 STDMETHOD_(ULONG, AddRef)() { return 1; }
 STDMETHOD(QueryInterface)(void* obj) { return 0; }
 static DWORD WINAPI Thread(void* arg) { Target(); return 0; }
 static void Target() {}
};'''
facts = run(dict(files=[source('Win.h', cpp)]))
assert not any(i['relation'] == 'PARSE_ERROR' for i in facts['issues']), facts['issues']
assert any('Advise' in n['label'] for n in facts['nodes'])
assert any('Thread' in n['label'] for n in facts['nodes'])
assert any(e['relation'] == 'CALLS' for e in facts['edges'])
assert any(i['relation'] == 'SYNTAX_RECOVERY' for i in facts['issues'])
header = run(dict(files=[source('Helper.h', 'void Copy(const Thing& source); void Log(bool cr = true);')]))
assert not any(i['relation'] == 'PARSE_ERROR' for i in header['issues'])
print('Pointer/soft-keyword declarations, call preservation, Windows annotation and header checks passed')
