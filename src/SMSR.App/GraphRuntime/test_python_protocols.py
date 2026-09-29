"""Protocol operands are candidates, not runtime method or heap resolution."""
import json
from test_python_values import model, reaches


def verify():
    source = '''def f(items,obj):
 for a,b in items:
  if a: continue
  obj.send(b, original=a)
 return obj.field
'''
    data = model(source)
    assert data['status']=='VALUE_FLOW_CANDIDATES', data['reason']
    nodes = data['nodes']
    origin = next(n['id'] for n in nodes if n['kind']=='PARAMETER' and n['name']=='items')
    args = [n for n in nodes if n['kind']=='CALL_ARGUMENT']
    assert len(args)==2 and all(reaches(data,origin,n['id']) for n in args)
    receiver = next(n for n in nodes if n['kind']=='CALL_RECEIVER_CANDIDATE')
    obj = next(n['id'] for n in nodes if n['kind']=='PARAMETER' and n['name']=='obj')
    assert reaches(data,obj,receiver['id']) and not reaches(data,origin,receiver['id'])
    assert not any(reaches(data,origin,n['id']) for n in nodes if n['kind']=='OPAQUE_CALL_RESULT')
    parts = [n for n in nodes if n['kind']=='UNPACKED_ITEM']
    for part,name in zip(parts,('a','b')):
        write = next(n for n in nodes if n['kind']=='WRITE' and n['name']==name)
        assert part['itemIndex']==('a','b').index(name) and reaches(data,part['id'],write['id'])
    assert json.dumps(data)==json.dumps(model(source))
    assert '<method-receiver>' not in json.dumps(data) and '<call-null>' not in json.dumps(data)
    for body in ('return obj.send(x)', 'method=obj.send\n return method(x)',
                 'return obj.first(x).second(x)', 'return target(obj.send(x), named=x)'):
        data = model('def f(obj,x):\n '+body)
        assert data['status']=='VALUE_FLOW_CANDIDATES', (body,data['reason'])
        ids = {n['id'] for n in data['nodes']}
        assert all(e['source'] in ids and e['target'] in ids for e in data['edges'])
        x = next(n['id'] for n in data['nodes'] if n['kind']=='PARAMETER' and n['name']=='x')
        assert not any(reaches(data,x,n['id']) for n in data['nodes'] if n['kind']=='RETURN')
    from test_python_iteration import verify as iteration
    iteration()
    print('Python iterator/method protocol self-check passed')


if __name__=='__main__':
    verify()
