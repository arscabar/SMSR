from typescript_bundle import run


def verify():
    source='''
function early(mode:number,a:number,b:number){switch(mode){case 0:return a;default:return b;}}
function killed(mode:number,a:number){switch(mode){case 0:a=0;break;default:a=1;}return a;}
function wrap(mode:number,a:number,b:number){return early(mode,a,b);}
function erased(mode:number,a:number){return killed(mode,a);}
function only(a:number){switch(a){default:return a;}}
function empty(a:number){switch(a=0){}return a;}
function nested(mode:number,a:number,other:number){let x=0;switch(mode){case 0:switch(other){case 1:x=a;break;default:break;}break;default:break;}return x;}
function continuing(mode:number,a:number){let x=0;for(;;x=a){switch(mode){case 0:continue;default:break;}break;}return x;}
function breaking(mode:number,a:number){let x=0;for(;;x=a){switch(mode){default:break;}break;}return x;}
function scope(mode:number,a:number){switch(mode){case 0:{let a=0;break;}default:break;}return a;}
function first(mode:number,a:number,b:number){let x=a;switch(mode){case (x=0):return x;case (x=b):break;}return x;}
function implicit(mode:number,a:number){switch(mode){case 0:return a;}}
function returned(mode:number,a:number){switch(mode){case 0:return a;default:return 0;}return 9;}
declare function opaque(a:number):number;
function unknown(mode:number,a:number){switch(mode){default:return opaque(a);}}
function unsupported(mode:number,a:number){switch(mode){case 0:try{return a;}finally{}}}
'''
    r=run(dict(files=[dict(path='switch_paths.ts',text=source)]))
    assert r['status']=='COMPILER_BINDINGS',r['diagnostics']
    assert r==run(dict(files=[dict(path='switch_paths.ts',text=source)]))
    expected=[[1,2],[],[1,2],[],[0],[],[1],[1],[],[1],[2],[1],[1],[],None]
    assert len(r['functions'])==len(expected)
    for f,wanted in zip(r['functions'],expected):
        s=f['valueSummary']
        if wanted is None:
            assert s['status']=='UNAVAILABLE';continue
        assert s['status']=='DATA_DEPENDENCY_CANDIDATES'
        actual=sorted({i for out in s['returns'] for i in out['parameterIndices']})
        assert actual==wanted,(f['name'],actual,wanted)
        assert any(out['unknownValueIds'] for out in s['returns'])==(f['name']=='unknown')
    fs={f['name']:f for f in r['functions']}
    assert len(fs['wrap']['valueSummary']['callEdges'])==2
    assert not fs['erased']['valueSummary']['callEdges']
    assert len(fs['implicit']['valueSummary']['returns'])==2
    assert len(fs['returned']['valueSummary']['returns'])==2
    assert [out['parameterIndices'] for out in fs['first']['valueSummary']['returns']]==[[],[2]]
