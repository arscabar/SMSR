"""Independent expected parameter dependence; never execute input programs."""
from typescript_bundle import run


def main():
    source = '''function pick(a:number,b:number){return a;}
function kill(a:number){a=0;return a;}
function chain(a:number,b:number){return pick(kill(a),b);}
function separate(a:number,b:number){let x=pick(a,0);let y=pick(b,0);return y;}
function middle(a:number,b:number){return pick(b,a);}
function outer(a:number,b:number){return middle(b,a);}
function recursive(a:number,p:boolean):number{if(p)return a;return recursive(a,true);}
function mutualA(a:number,p:boolean):number{if(p)return a;return mutualB(a,p);}
function mutualB(a:number,p:boolean):number{return mutualA(a,p);}
declare function opaque(a:number):number;
function unknown(a:number){return opaque(a);}
function transitive(a:number){return unknown(a);}
function unused(a:number){opaque(a);return 0;}
function implicit(a:number){pick(a,0);}
function unavailable(a:number){try{return a;}finally{}}
function boundary(a:number){return unavailable(a);}
'''
    r = run({'files': [{'path': 'summary.ts', 'text': source}]})
    assert r['status'] == 'COMPILER_BINDINGS', r['diagnostics']
    expected = [[0],[],[],[1],[1],[0],[0],[0],[0],[],[],[],[],None,[]]
    assert len(r['functions']) == len(expected)
    for f, wanted in zip(r['functions'], expected):
        s = f['valueSummary']
        if wanted is None:
            assert s['status'] == 'UNAVAILABLE' and not s['returns']; continue
        assert s['status'] == 'DATA_DEPENDENCY_CANDIDATES'
        actual = sorted({i for out in s['returns'] for i in out['parameterIndices']})
        assert actual == wanted, (f['line'], actual, wanted)
        unknown = any(out['unknownValueIds'] for out in s['returns'])
        assert unknown == (f['line'] in (11,12,16)), (f['line'], s)
        ids = {n['id'] for n in f['values']['nodes']}
        assert all(e['source'] in ids and e['target'] in ids for e in s['callEdges'])
    isolated = r['functions'][3]['valueSummary']['callEdges']
    assert len(isolated) == 2 and len({e['target'] for e in isolated}) == 2
    print('TypeScript multi-hop/recursive/call-isolation/unknown return summaries passed')


if __name__ == '__main__':
    main()
