from itertools import permutations
from python_compiler import analyze
from python_keyword_bindings import bind


def verify():
    # All 24 keyword orders x 4 returned formal slots; expect caller index by name.
    for order in permutations(range(4)):
        for picked in range(4):
            names='xyzw';arguments=','.join(f'{names[i]}={"abcd"[3-i]}' for i in order)
            source=f'def f(a,b,c,d):\n def inner(x,y,*,z,w): return {names[picked]}\n return inner({arguments})'
            s=analyze(source,'oracle.py')['functions'][1]['valueSummary']
            target=s['calls'][0]['localTarget']
            assert [a['parameterIndex'] for a in target['arguments']]==list(order)
            assert s['returns'][0]['parameterIndices']==[3-picked]
            assert not s['returns'][0]['unknownValueIds']
            assert len(s['callEdges'])==1
    source='def f(a):\n def inner(*,입력): return 입력\n return inner(입력=a)'
    s=analyze(source,'unicode.py')['functions'][1]['valueSummary']
    assert s['returns'][0]['parameterIndices']==[0]
    params=[dict(index=0,name='x',kind='POSITIONAL_OR_KEYWORD',valueId='p0')]
    args=[dict(keyword=True,valueId='arg0')]
    assert bind(params,args,None,lambda _:None,lambda _:None)[1]=='KEYWORD_METADATA_UNAVAILABLE'
    def reject(_):raise ValueError('keyword budget')
    for spend,work in ((reject,lambda _:None),(lambda _:None,reject)):
        try:bind(params,args,('x',),spend,work)
        except ValueError as e:assert str(e)=='keyword budget'
        else:raise AssertionError('Missing keyword binding budget')
