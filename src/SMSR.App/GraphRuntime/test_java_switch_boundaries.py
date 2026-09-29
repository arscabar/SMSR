"""Rule isolation, nested transfers, direct-call summaries and refusal boundaries."""
from test_java_bundle import analyze
from test_java_switch_paths import trace


def verify():
    for default in (-1,0,1,2):
        labels=[0,1]
        if default>=0:labels.insert(default,None)
        parts=[('default' if k is None else f'case {k}')+f' -> x=p{i};' for i,k in enumerate(labels)]
        line='static int f(int k,int p0,int p1,int p2){int x=0;switch(k){'+''.join(parts)+'}return x;}'
        r=analyze({'Rules.java':'class Rules {\n'+line+'\n}'})
        assert r['status']=='BOUND_INPUT_BUNDLE',r['diagnostics']
        m=r['methods'][0]
        for key in (0,1,9):
            at=labels.index(key) if key in labels else labels.index(None) if None in labels else -1
            assert trace(m['control'],line,key)==([] if at<0 else [f'x=p{at}'],1)
    source='''class More {
static int choose(int k,int a,int b){int x=0;switch(k){case 0:x=a;break;default:x=b;}return x;}
static int relay(int k,int a,int b){return choose(k,a,b);}
static int erase(int k,int a){switch(k){case 0:a=0;break;default:a=1;}return a;}
static int killed(int k,int a){return erase(k,a);}
static int resumed(int k,int a){int x=0;for(int i=0;i<1;x=a){i++;switch(k){case 0:continue;default:break;}x=0;}return x;}
static int broken(int k,int a){int x=0;for(;;x=a){switch(k){default:break;}break;}return x;}
static int throwing(int k,int a){switch(k){case 0:throw new RuntimeException();default:return a;}}
}'''
    r=analyze({'More.java':source});assert r['status']=='BOUND_INPUT_BUNDLE'
    for m,want in zip(r['methods'],([1,2],[1,2],[],[],[1],[],None)):
        s=m['valueSummary']
        if want is None:
            assert s['status']=='UNAVAILABLE' and m['control']['status']=='NORMAL_CFG_CONTROL_DEPENDENCE';continue
        assert sorted({i for out in s['returns'] for i in out['parameterIndices']})==want,m
    assert len(r['methods'][1]['valueSummary']['callEdges'])==2
    assert not r['methods'][3]['valueSummary']['callEdges']
    bad=analyze({'Bad.java':'class Bad {int f(int k){switch(k){case 0:break;case 0:break;}return k;}}'})
    assert bad['status']!='BOUND_INPUT_BUNDLE'
    assert all(m['control']['status']==m['values']['status']=='UNAVAILABLE' for m in bad['methods'])
