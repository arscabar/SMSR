"""Function object provenance is not spelling-based resolution."""
from python_compiler import analyze

SOURCE='''
def alias(a):
 def inner(x): return x
 copied=inner
 return copied(a)
def overwritten(a, external):
 def inner(x): return x
 inner=external
 return inner(a)
def ambiguous(a,flag):
 def left(x): return x
 def right(x): return 0
 chosen=left if flag else right
 return chosen(a)
def mixed(a,flag,external):
 def inner(x): return x
 chosen=inner if flag else external
 return chosen(a)
def container(a):
 def inner(x): return x
 return [inner][0](a)
def defaults(a):
 def inner(x=99123): return x
 return inner(a),inner()
def keywords(a):
 def inner(x): return x
 return inner(x=a)
def decorated(a,decorate):
 @decorate
 def inner(x): return x
 return inner(a)
def global_name(a): return alias(a)
def cell(a):
 def inner(): return a
 return inner()
'''


def verify():
    result=analyze(SOURCE,'calls.py');assert result==analyze(SOURCE,'calls.py')
    fs={f['name']:f for f in result['functions']}
    def calls(name):return fs[name]['valueSummary']['calls']
    local=calls('alias')[0]['localTarget'];body=fs['alias.<locals>.inner']
    assert local['status']=='LOCAL_BODY_CONNECTION_CANDIDATE' and local['bodyIds']==[body['id']]
    assert local['arguments'][0]['parameterValueId']==body['valueSummary']['parameters'][0]['valueId']
    assert local['returns'][0]['returnValueId']==body['valueSummary']['returns'][0]['valueId']
    for name in ('overwritten','container','global_name'):
        assert calls(name)[0]['localTarget']['status']=='UNRESOLVED',name
    assert fs['decorated']['values']['status']=='UNAVAILABLE'
    assert calls('ambiguous')[0]['localTarget']['status']=='AMBIGUOUS'
    assert len(calls('ambiguous')[0]['localTarget']['bodyIds'])==2
    assert calls('mixed')[0]['localTarget']['hasUnknown']
    assert calls('defaults')[0]['localTarget']['status']=='LOCAL_BODY_CONNECTION_CANDIDATE'
    assert calls('defaults')[1]['localTarget']['reason']=='ARGUMENT_BINDING_UNSUPPORTED'
    assert calls('keywords')[0]['localTarget']['status']=='LOCAL_BODY_CONNECTION_CANDIDATE'
    assert fs['cell']['values']['status']=='UNAVAILABLE'
    assert fs['alias']['valueSummary']['returns'][0]['parameterIndices']==[0]
    assert not fs['alias']['valueSummary']['returns'][0]['unknownValueIds']
    from test_python_local_boundaries import verify as boundaries
    boundaries()
    print('Python local function origins, aliases, ambiguity, overwrite and binding boundaries passed')


if __name__=='__main__':verify()
