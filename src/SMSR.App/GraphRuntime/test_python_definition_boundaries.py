from python_compiler import analyze


def verify():
    source = '''def protected(flag):
 x = 1
 try:
  x = risky()
 except Exception:
  return x
 return x
def closure(x):
 def nested(): return x
 return nested
def comprehension(items):
 return [x for x in items]
def stream(x):
 yield x
 x = 2
 yield x
'''
    functions = {f['name']: f for f in analyze(source,'boundary.py')['functions']}
    protected = functions['protected']['definitions']
    assert protected['status']=='PARTIAL_SLOT_DEFINITIONS'
    assert 'EXCEPTION_BEFORE_OR_AFTER_CANDIDATES' in protected['boundaries']
    read = next(r for r in protected['reads'] if r['name']=='x' and r['source']['start']['line']==5)
    ids = {e['source'] for e in protected['edges'] if e['target']==read['id']}
    origins = [d for d in protected['definitions'] if d['id'] in ids]
    assert {d['source']['start']['line']+1 for d in origins}=={2,4}
    closure = functions['closure']['definitions']
    assert closure['status']=='PARTIAL_SLOT_DEFINITIONS'
    assert all(r['name']!='x' for r in closure['reads'])
    assert functions['comprehension']['definitions']['reason']=='STACK_SAVED_SLOT_RESTORATION'
    stream = functions['stream']['definitions']
    assert stream['status']=='PARTIAL_SLOT_DEFINITIONS'
    for f in functions.values():
        d=f['definitions']; definitions={x['id']:x for x in d['definitions']}; reads={x['id']:x for x in d['reads']}
        assert all(definitions[e['source']]['bindingId']==reads[e['target']]['bindingId'] for e in d['edges'])
    try:
        from python_reaching import solve
        remaining=1
        def work(n):
            nonlocal remaining
            remaining-=n
            if remaining<0: raise ValueError('limit')
        solve(0,{0:{1},1:{0}}, {}, {}, work)
        raise AssertionError('Work cap missing')
    except ValueError:
        pass
