"""Parameter dependence and opaque call boundaries have independent expectations."""
import json
from python_compiler import analyze

SOURCE='''
def identity(a): return a
def killed(a):
 a=0
 return a
def choose(flag,a,b): return a if flag else b
def loop(flag,a):
 x=0
 while flag:
  x=a
  flag=False
 return x
def slots(a,/,b=98765,*args,c,**kwargs): return (a,b,c,args,kwargs)
def call(a,b,target): return target(a,named=b)
def separate(a,b,target):
 target(a)
 return target(b)
def unused(a,target):
 target(a)
 return 0
def unbound(flag,a):
 if flag: x=a
 return x
def deleted(flag,a):
 x=a
 if flag: del x
 return x
def protocol(items,receiver):
 for item in items: receiver.send(item)
 return receiver.result
def secret(target): return target("SUMMARY_SECRET",hidden_keyword=1)
def unavailable(a):
 try: return a
 finally: pass
'''


def verify():
    result=analyze(SOURCE,'summary.py')
    assert result==analyze(SOURCE,'summary.py')
    functions={f['name']:f for f in result['functions']}
    expected=dict(identity=[0],killed=[],choose=[1,2],loop=[1],slots=list(range(5)),
                  call=[],separate=[],unused=[],unbound=[1],deleted=[1],protocol=[1],secret=[])
    uncertain={'call','separate','unbound','deleted','protocol','secret'}
    for name,wanted in expected.items():
        s=functions[name]['valueSummary'];assert s['status']=='DATA_DEPENDENCY_CANDIDATES'
        actual=sorted({p for r in s['returns'] for p in r['parameterIndices']})
        assert actual==wanted,(name,actual,wanted)
        assert any(r['unknownValueIds'] for r in s['returns'])==(name in uncertain),name
    assert [p['kind'] for p in functions['slots']['valueSummary']['parameters']]==[
        'POSITIONAL_ONLY','POSITIONAL_OR_KEYWORD','KEYWORD_ONLY','VAR_POSITIONAL','VAR_KEYWORD']
    call=functions['call']['valueSummary']['calls'][0]
    assert call['target']['parameterIndices']==[2] and call['receiver'] is None
    assert [(a['index'],a['keyword'],a['parameterIndices']) for a in call['arguments']]==[(0,False,[0]),(1,True,[1])]
    s=functions['separate']['valueSummary']
    assert s['returns'][0]['unknownValueIds']==[s['calls'][1]['resultValueId']]
    serialized=json.dumps([f['valueSummary'] for f in functions.values()])
    assert all(secret not in serialized for secret in ('SUMMARY_SECRET','hidden_keyword','98765'))
    assert functions['unavailable']['valueSummary']['status']=='UNAVAILABLE'
    from test_python_summary_boundaries import verify as boundaries
    boundaries(functions)
    print('Python input/return/call summaries, slot kinds, unknown isolation and budgets passed')


if __name__=='__main__':verify()
