"""Nested yield ownership, abrupt boundaries, invalid compilation and privacy."""
from test_java_bundle import analyze
from test_java_yield_paths import trace

def verify():
    line='static int f(int k,int a,int b){int x=0;int y=switch(k){default -> {int z=switch(k){default -> {x=a;yield x;}};x=b;yield x;}};return y;}'
    r=analyze({'Nested.java':'class Nested {\n'+line+'\n}'})
    m=r['methods'][0]
    assert trace(m['control'],line,0)==(['x=a','x=b'],2,2)
    assert {i for o in m['valueSummary']['returns'] for i in o['parameterIndices']}=={2}
    value=m['values'];ids={n['id']:n for n in value['nodes']}
    yields=[n['id'] for n in value['nodes'] if n['kind']=='YIELD']
    targets=[e['target'] for e in value['edges'] if e['source'] in yields and e['relation']=='SWITCH_RESULT_VALUE']
    assert len(yields)==len(set(targets))==2
    assert all(ids[t]['kind']=='SWITCH_RESULT' for t in targets)
    source='''class Edge {
static int f(int k,int a){return switch(k){case 0 -> a;default -> throw new RuntimeException();};}
static int unsupported(int k,int a){return switch(k){default -> {try{yield a;}finally{a=0;}}};}
}'''
    r=analyze({'Edge.java':source});assert r['status']=='BOUND_INPUT_BUNDLE'
    assert r['methods'][0]['values']['status']=='UNAVAILABLE'
    c=r['methods'][0]['control'];assert c['status']=='NORMAL_CFG_CONTROL_DEPENDENCE'
    throws={n['id'] for n in c['nodes'] if n['kind']=='EXPLICIT_THROW'}
    assert throws and all(e['target']==-1 for e in c['transfers'] if e['source'] in throws)
    assert r['methods'][1]['values']['status']==r['methods'][1]['control']['status']=='UNAVAILABLE'
    for body in ('return switch(k){case 0 -> 1;};',
                 'return switch(k){default -> {break;}};',
                 'return switch(k){default -> {yield 1;yield 2;}};'):
        bad=analyze({'Bad.java':'class Bad {int f(int k){'+body+'}}'})
        assert bad['status']!='BOUND_INPUT_BUNDLE'
        assert all(m['control']['status']==m['values']['status']=='UNAVAILABLE' for m in bad['methods'])
