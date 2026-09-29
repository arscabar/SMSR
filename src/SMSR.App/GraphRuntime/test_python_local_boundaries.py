from itertools import product
from python_compiler import analyze
from python_local_calls import connect


def verify():
    source='';expected={}
    for mask in range(32):
        lines=[' def left(x): return x',' def right(x): return 0',' chosen=left']
        for i in range(5):lines.append(f' if p{i}: chosen='+('right' if mask&(1<<i) else 'external'))
        source+=f'def f{mask}(a,external,p0,p1,p2,p3,p4):\n'+'\n'.join(lines)+'\n return chosen(a)\n'
        values=set()
        for flags in product((False,True),repeat=5):
            value='left'
            for i,flag in enumerate(flags):
                if flag:value='right' if mask&(1<<i) else '?'
            values.add(value)
        expected[f'f{mask}']=values
    fs=analyze(source,'oracle.py')['functions'];names={f['id']:f['name'].split('.')[-1] for f in fs}
    for f in fs:
        if f['name'] not in expected:continue
        target=f['valueSummary']['calls'][0]['localTarget']
        actual={names[i] for i in target['bodyIds']}
        if target['hasUnknown']:actual.add('?')
        assert actual==expected[f['name']],(f['name'],actual)
    text='''
def outer(a):
 def inner(x): return x
 copied=inner
 while a:
  copied=inner
  a=0
 return copied(a),copied(a)
'''
    fs=analyze(text,'loop.py')['functions'];calls=fs[1]['valueSummary']['calls']
    assert all(c['localTarget']['status']=='LOCAL_BODY_CONNECTION_CANDIDATE' for c in calls)
    assert calls[0]['localTarget']['arguments'][0]['argumentValueId']!=calls[1]['localTarget']['arguments'][0]['argumentValueId']
    assert calls[0]['localTarget']['returns'][0]['resultValueId']!=calls[1]['localTarget']['returns'][0]['resultValueId']
    for declaration,reason in (
            ('def inner(*x): return x','ARGUMENT_BINDING_UNSUPPORTED'),
            ('def inner(x):\n  try: return x\n  finally: pass','BODY_VALUES_UNAVAILABLE')):
        sample='def f(a):\n '+declaration+'\n return inner(a)\n'
        target=analyze(sample,'limits.py')['functions'][1]['valueSummary']['calls'][0]['localTarget']
        assert target['reason']==reason and not target['arguments'] and not target['returns']
    annotated='def f(a):\n def inner(x:int)->int: return x\n return inner(a)'
    target=analyze(annotated,'annotations.py')['functions'][1]['valueSummary']['calls'][0]['localTarget']
    assert target['status']=='LOCAL_BODY_CONNECTION_CANDIDATE'
    def reject(_):raise ValueError('local budget')
    for spend,work in ((reject,lambda _:None),(lambda _:None,reject)):
        try:connect(fs,spend,work)
        except ValueError as e:assert str(e)=='local budget'
        else:raise AssertionError('Missing local call budget')
