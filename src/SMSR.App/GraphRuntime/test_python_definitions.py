from python_compiler import analyze


def functions(source):
    return {f['name']: f for f in analyze(source, 'defs.py')['functions']}


def candidates(function, name, line):
    data = function['definitions']
    reads = [r for r in data['reads'] if r['name']==name and r['source']['start']['line']==line-1]
    assert reads
    # CPython may duplicate one source return into separate branch tails.
    targets = {r['id'] for r in reads}
    ids = {e['source'] for e in data['edges'] if e['target'] in targets}
    return dict(mayBeUnbound=any(r['mayBeUnbound'] for r in reads)), [d for d in data['definitions'] if d['id'] in ids]


def main():
    f = functions('''def overwrite(x):
 x = 1
 return x
def branch(flag):
 if flag: x = 1
 else: x = 2
 return x
def loop(x):
 while x < 3: x += 1
 return x
def unbound(flag):
 if flag: x = 1
 return x
def deleted(x):
 del x
 return x
def args(a, /, b=1, *items, c=2, **options):
 return a,b,items,c,options
''')
    _, defs = candidates(f['overwrite'],'x',3)
    assert len(defs)==1 and defs[0]['kind']=='WRITE'
    _, defs = candidates(f['branch'],'x',7)
    assert {d['source']['start']['line']+1 for d in defs} == {5,6}
    _, defs = candidates(f['loop'],'x',10)
    assert {d['kind'] for d in defs} == {'PARAMETER','WRITE'}
    read, defs = candidates(f['unbound'],'x',13)
    assert read['mayBeUnbound'] and {d['kind'] for d in defs}=={'WRITE','UNBOUND_ENTRY'}
    read, defs = candidates(f['deleted'],'x',16)
    assert read['mayBeUnbound'] and [d['kind'] for d in defs]==['UNBOUND_DELETE']
    for name in ('a','b','items','c','options'):
        _, defs = candidates(f['args'],name,18)
        assert [d['kind'] for d in defs]==['PARAMETER']
    from test_python_definition_boundaries import verify
    verify()
    from test_python_reaching import verify as oracle
    oracle()
    print('Python reaching-definition self-check passed')


if __name__=='__main__':
    main()
