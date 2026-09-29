"""Check producer paths, joins, kills and opaque call boundaries."""
import json
from python_compiler import analyze


def model(source, name='f'):
    function = next(f for f in analyze(source, 'values.py')['functions'] if f['name']==name)
    return function['values']


def reaches(data, start, end):
    pending, seen = [start], {start}
    while pending:
        current = pending.pop()
        if current == end:
            return True
        for edge in data['edges']:
            if edge['source']==current and edge['target'] not in seen:
                seen.add(edge['target'])
                pending.append(edge['target'])
    return False


def verify():
    for expression in ('x+1', '-x', 'not x', '~x', 'x[0]', '[x]', '(x,)',
                       '{x}', 'x is None', 'x in container', 'x and 1', 'x or 1'):
        data = model(f'def f(x):\n y = {expression}\n return y')
        assert data['status']=='VALUE_FLOW_CANDIDATES', (expression, data)
        origin = next(n['id'] for n in data['nodes'] if n['kind']=='PARAMETER')
        returns = [n['id'] for n in data['nodes'] if n['kind']=='RETURN']
        assert any(reaches(data, origin, n) for n in returns), expression
    data = model('def f(x):\n x = 7\n return x')
    origin = next(n['id'] for n in data['nodes'] if n['kind']=='PARAMETER')
    assert not any(reaches(data, origin, n['id']) for n in data['nodes'] if n['kind']=='RETURN')
    data = model('def f(flag,a,b):\n return (a if flag else b)+1')
    for name in ('a','b'):
        origin = next(n['id'] for n in data['nodes'] if n['kind']=='PARAMETER' and n['name']==name)
        assert any(reaches(data, origin, n['id']) for n in data['nodes'] if n['kind']=='RETURN')
    data = model('def f(x):\n while x < 3: x += 1\n return x')
    assert data['status']=='VALUE_FLOW_CANDIDATES'
    assert any(e['relation']=='ASSIGNMENT_INPUT' for e in data['edges'])
    data = model('def f(x, target):\n return target(x+1, named=x)')
    assert data['status']=='VALUE_FLOW_CANDIDATES'
    args = [n for n in data['nodes'] if n['kind']=='CALL_ARGUMENT']
    assert [(n['argument'],n['keyword']) for n in args]==[(0,False),(1,True)]
    origin = next(n['id'] for n in data['nodes'] if n['kind']=='PARAMETER' and n['name']=='x')
    assert all(reaches(data,origin,n['id']) for n in args)
    assert not any(reaches(data,origin,n['id']) for n in data['nodes'] if n['kind']=='RETURN')
    from test_python_value_boundaries import verify as boundaries
    boundaries()
    print('Python value-flow self-check passed')


if __name__=='__main__':
    verify()
