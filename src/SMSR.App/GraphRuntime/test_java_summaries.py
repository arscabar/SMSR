"""Independent data-dependency expectations across javac-resolved calls."""
from test_java_bundle import analyze


def main():
    source = '''class Summary {
static int pick(int a,int b){return a;}
static int kill(int a){a=0;return a;}
static int chain(int a,int b){return pick(kill(a),b);}
static int separate(int a,int b){int x=pick(a,0);int y=pick(b,0);return y;}
static int middle(int a,int b){return pick(b,a);}
static int outer(int a,int b){return middle(b,a);}
static int recursive(int a,boolean p){if(p)return a;return recursive(a,true);}
static int mutualA(int a,boolean p){if(p)return a;return mutualB(a,p);}
static int mutualB(int a,boolean p){return mutualA(a,p);}
static int unknown(int a){return Math.abs(a);}
static int transitive(int a){return unknown(a);}
static int unused(int a){Math.abs(a);return 0;}
static void implicit(int a){pick(a,0);}
static int unavailable(int a){try{return a;}finally{}}
static int boundary(int a){return unavailable(a);}
}'''
    r=analyze({'Summary.java':source})
    assert r['status']=='BOUND_INPUT_BUNDLE',r['diagnostics']
    expected=[[0],[],[],[1],[1],[0],[0],[0],[0],[],[],[],[],None,[]]
    assert len(r['methods'])==len(expected)
    for index,(m,wanted) in enumerate(zip(r['methods'],expected)):
        s=m['valueSummary']
        if wanted is None:
            assert s['status']=='UNAVAILABLE' and not s['returns'];continue
        assert s['status']=='DATA_DEPENDENCY_CANDIDATES'
        actual=sorted({i for out in s['returns'] for i in out['parameterIndices']})
        assert actual==wanted,(m['id'],actual,wanted)
        assert any(out['unknownValueIds'] for out in s['returns'])==(index in (9,10,14)),m
        ids={n['id'] for n in m['values']['nodes']}
        assert all(e['source'] in ids and e['target'] in ids for e in s['callEdges'])
    isolated=r['methods'][3]['valueSummary']['callEdges']
    assert len(isolated)==2 and len({e['target'] for e in isolated})==2
    for c in r['connections']:
        if c['status']=='STATIC_BODY_CANDIDATE':
            assert c['inputs'] and c['returns'] and c['callValueId']
    from test_java_summary_boundaries import verify
    verify(analyze)
    print('Java multi-hop/recursive/call-isolation/unknown summaries and dispatch boundaries passed')


if __name__ == '__main__':
    main()
