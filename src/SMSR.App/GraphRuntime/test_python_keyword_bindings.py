"""Keyword names bind transiently; bad calls never acquire data edges."""
import json
from python_compiler import analyze

SOURCE='''
def reordered(a,b):
 def inner(x,y): return y
 return inner(y=a,x=b)
def mixed(a,b):
 def inner(x,/,*,y): return y
 return inner(b,y=a)
def only(a):
 def inner(*,x): return x
 return inner(x=a)
def default(a):
 def inner(x=81927): return x
 return inner(x=a)
def nested(a,b):
 def inner(x): return x
 def outer(x,*,y): return x
 return outer(inner(x=b),y=a)
def duplicate(a,b):
 def inner(x,y): return x
 return inner(a,x=b)
def forbidden(a):
 def inner(x,/): return x
 return inner(x=a)
def missing(a):
 def inner(x,y=0): return x
 return inner(x=a)
def too_many(a):
 def inner(*,x): return x
 return inner(a)
def secret(a):
 def inner(x): return x
 return inner(PRIVATE_KEYWORD_5283="PRIVATE_LITERAL_7319")
'''


def verify():
    result=analyze(SOURCE,'keywords.py');assert result==analyze(SOURCE,'keywords.py')
    fs={f['name']:f['valueSummary'] for f in result['functions']}
    for name,wanted in dict(reordered=[0],mixed=[0],only=[0],default=[0],nested=[1]).items():
        s=fs[name]
        assert sorted({p for r in s['returns'] for p in r['parameterIndices']})==wanted,name
        assert not any(r['unknownValueIds'] for r in s['returns']),name
    assert [a['parameterIndex'] for a in fs['reordered']['calls'][0]['localTarget']['arguments']]==[1,0]
    for name,reason in dict(duplicate='DUPLICATE_ARGUMENT',forbidden='UNKNOWN_OR_POSITIONAL_ONLY_KEYWORD',
            missing='MISSING_ARGUMENT',too_many='TOO_MANY_POSITIONAL_ARGUMENTS',secret='UNKNOWN_OR_POSITIONAL_ONLY_KEYWORD').items():
        s=fs[name];target=s['calls'][0]['localTarget']
        assert target['bindingReason']==reason,(name,target)
        assert not target['arguments'] and not target['returns'] and not s['callEdges']
        assert s['returns'][0]['unknownValueIds']
    text=json.dumps(result)
    assert all(s not in text for s in ('PRIVATE_KEYWORD_5283','PRIVATE_LITERAL_7319','81927','call_keywords'))
    from test_python_keyword_oracle import verify as oracle
    oracle()
    print('Python positional/keyword-only/reordered binding, invalid calls, privacy and independent oracle passed')


if __name__=='__main__':verify()
