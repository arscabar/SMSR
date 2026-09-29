"""Known body returns are composed, not all call arguments unioned."""
from python_compiler import analyze

SOURCE='''
def relay(a):
 def inner(x): return x
 return inner(a)
def erase(a):
 def inner(x):
  x=0
  return x
 return inner(a)
def separate(a,b):
 def inner(x): return x
 inner(a)
 return inner(b)
def reorder(a,b):
 def inner(x,y): return y
 return inner(b,a)
def nested(a):
 def middle(x):
  def inner(y): return y
  return inner(x)
 return middle(a)
def uncertain(a):
 def inner(x): return external(x)
 return inner(a)
def unused(a):
 def inner(x):
  external(x)
  return 0
 return inner(a)
def gated(flag,a,b):
 def inner(condition,x,y): return x if condition else y
 return inner(flag,a,b)
def unsupported(a):
 def inner(x):
  try: return x
  finally: pass
 return inner(a)
def recursive(a):
 def inner(x): return inner(x)
 return inner(a)
def composed(a,b):
 def first(x): return x
 def second(x): return x
 return second(first(b))
'''


def verify():
    result=analyze(SOURCE,'summary.py');assert result==analyze(SOURCE,'summary.py')
    fs={f['name']:f for f in result['functions']}
    for name,wanted in dict(relay=[0],erase=[],separate=[1],reorder=[0],nested=[0],
                            uncertain=[],unused=[],gated=[1,2],unsupported=[],composed=[1]).items():
        s=fs[name]['valueSummary']
        actual=sorted({p for r in s['returns'] for p in r['parameterIndices']})
        assert actual==wanted,(name,actual)
        assert any(r['unknownValueIds'] for r in s['returns'])==(name in {'uncertain','unsupported'}),name
    assert fs['recursive']['valueSummary']['status']=='UNAVAILABLE'
    assert not fs['erase']['valueSummary']['callEdges']
    s=fs['separate']['valueSummary'];edges=s['callEdges']
    assert len(edges)==2 and edges[0]['target']!=edges[1]['target']
    assert edges[0]['source']!=edges[1]['source']
    inner=fs['uncertain.<locals>.inner']['valueSummary']['calls'][0]['resultValueId']
    assert fs['uncertain']['valueSummary']['returns'][0]['unknownValueIds']==[inner]
    from test_python_call_summary_oracle import verify as oracle
    oracle()
    print('Python call-composed data summaries: relay/erase/multi-hop/isolation/unknown/boundary checks passed')


if __name__=='__main__':verify()
