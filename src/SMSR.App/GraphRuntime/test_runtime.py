"""Run with the installed graph-runtime Python; no source leaves this machine."""
from cypher_guard import validate
from cypher_query import run as query
from symbols import run as analyze

for unsafe in ["CREATE (n)", "MATCH(n) DELETE n", "RETURN read_csv('secret')", "CALL show_tables() RETURN *", "RETURN 1; RETURN 2", "RETURN `read_csv`('secret')", "INSTALL httpfs", "RETURN 'unterminated", "RETURN load_extension('x')"]:
    try:
        validate(unsafe)
        raise AssertionError(unsafe)
    except ValueError:
        pass
validate("MATCH(n) WHERE n.label = 'DELETE; CALL' RETURN count(n)")
nodes = [dict(nodeId=x, label=x, kind='file', sourcePath=x+'.py', line=1) for x in ['a','b']]
nodes[0]['label'] = '한글, 따옴표 "검사" 및 \\경로\n다음 줄'
edges = [dict(sourceId='a',targetId='b',relation='LINKS',confidence='EXTRACTED')]
result = query(dict(query='MATCH (a:Node)-[r:REL]->(b:Node) RETURN a.id, r.relation, b.id',nodes=nodes,edges=edges,revision=7))
assert result['rows'] == [['a','LINKS','b']] and result['revision'] == 7
assert query(dict(query='UNWIND range(1,300) AS x RETURN x',nodes=[],edges=[],revision=1))['truncated']
chain_nodes = [dict(nodeId=x, label=x, kind='file', sourcePath=x+'.py', line=1) for x in ['a','b','c']]
chain_edges = [dict(sourceId=a,targetId=b,relation='LINKS',confidence='EXTRACTED') for a,b in [('a','b'),('b','c')]]
for cypher, expected in [
    ("MATCH (a:Node {id:'a'})-[:REL*1..2]->(b:Node) RETURN b.id ORDER BY b.id", [['b'],['c']]),
    ("MATCH (a:Node {id:'a'})<-[:REL]-(b:Node) RETURN b.id", []),
    ("MATCH (n:Node) WITH n.kind AS kind, count(n) AS total WHERE total > 1 RETURN kind,total", [['file',3]]),
    ("MATCH (n:Node {id:'c'}) OPTIONAL MATCH (n)-[:REL]->(m:Node) RETURN n.id,m.id", [['c',None]]),
]:
    actual = query(dict(query=cypher,nodes=chain_nodes,edges=chain_edges,revision=9))
    assert actual['rows'] == expected, (cypher, actual)

cases = {
    'py': 'def f():\n    x = input()\n    eval(x)\n',
    'cs': 'class A { void F() { var x = Console.ReadLine(); Process.Start(x); } }',
    'ts': 'function f() { const x = request.query; eval(x); }',
    'js': 'function f() { const x = request.query; eval(x); }',
    'java': 'class A { void f() { String x = request.getParameter("q"); Runtime.exec(x); } }',
    'go': 'package main\nfunc f() { x := input(); exec(x) }',
    'rs': 'fn f() { let x = input(); exec(x); }',
    'cpp': 'void f() { auto x = input(); system(x); }',
}
for extension, source in cases.items():
    result = analyze(dict(path='sample.'+extension, source=source))
    assert result['symbols'], (extension, result)
    assert result['status'] == 'SYNTAX_ONLY', (extension, result)
    assert result['findings'], (extension, result)
assert not analyze(dict(path='safe.py',source='def f():\n    eval("constant")\n'))['findings']
assert analyze(dict(path='bad.py',source='def !!!'))['status'] == 'PARSE_ERRORS'
assert analyze(dict(path='other.xyz',source='test'))['status'] == 'UNSUPPORTED'
assert not analyze(dict(path='scopes.py',source='def a():\n    x = input()\ndef b():\n    eval(x)\n'))['findings']
print('Cypher safety, projection, cap, 8 language syntax/taint and negative checks passed')

if '--vectors' in __import__('sys').argv:
    from embedding import run
    import numpy as np
    vectors = np.asarray(run(dict(texts=['사용자 로그인 인증', 'user login authentication', 'banana fruit recipe']))['vectors'])
    assert vectors.shape == (3,384)
    vectors /= np.linalg.norm(vectors,axis=1,keepdims=True)
    assert vectors[0] @ vectors[1] > vectors[0] @ vectors[2]
    print('Offline Korean/English semantic ranking passed')
