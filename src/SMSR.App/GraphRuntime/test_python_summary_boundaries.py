from itertools import product
from python_compiler import analyze
from python_value_summary import analyze as summary


def verify(functions):
    s=functions['protocol']['valueSummary'];call=s['calls'][0]
    assert call['receiver']['parameterIndices']==[1]
    assert call['target']['parameterIndices']==[1] and call['target']['unknownValueIds']
    assert call['arguments'][0]['parameterIndices']==[0] and call['arguments'][0]['unknownValueIds']
    for f in functions.values():
        s=f['valueSummary'];ids={n['id'] for n in f['values']['nodes']}
        for r in s['returns']:
            assert r['valueId'] in ids and set(r['unknownValueIds'])<=ids
    code=compile('def f(a): return a','<test>','exec').co_consts[0]
    f=analyze('def f(a): return a','limit.py')['functions'][1]
    def reject(_):raise ValueError('summary budget')
    for spend,work in ((reject,lambda _:None),(lambda _:None,reject)):
        try:summary(code,f,spend,work)
        except ValueError as e:assert str(e)=='summary budget'
        else:raise AssertionError('Summary budget ignored')
    for body in ('yield a','return target(*a)','return [x for x in a]'):
        f=analyze('def f(a):\n '+body,'unsupported.py')['functions'][1]
        assert f['valueSummary']['status']=='UNAVAILABLE'
        assert not f['valueSummary']['returns'] and not f['valueSummary']['calls']
    # 32 overwrite programs, each with 32 independent branch outcomes.
    source='';expected={}
    for mask in range(32):
        body=[' x=0']
        for i in range(5):body.append(f' if p{i}: x='+('abc'[i%3] if mask&(1<<i) else '0'))
        source+=f'def f{mask}(a,b,c,p0,p1,p2,p3,p4):\n'+'\n'.join(body)+'\n return x\n'
        outcomes=[]
        for flags in product((False,True),repeat=5):
            state=None
            for i,flag in enumerate(flags):
                if flag:state=i%3 if mask&(1<<i) else None
            outcomes.append(state)
        expected[f'f{mask}']=sorted({i for i in outcomes if i is not None})
    for f in analyze(source,'oracle.py')['functions'][1:]:
        assert f['valueSummary']['returns'][0]['parameterIndices']==expected[f['name']]
