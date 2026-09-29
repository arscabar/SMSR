SOURCE='''
export function empty(){switch(d()){} }
export function only(){switch(d()){default:b0();} }
export function grouped(){switch(d()){case c0():case c1():b0();break;default:} }
export function nested(x:number,y:number){switch(x){case 0:switch(y){case 1:break;default:return 1;}b0();break;default:b1();}b2();}
export function loop(x:number){for(let i=0;i<2;i++){switch(x){case 0:continue;default:break;}b0();}return x;}
export function inside(x:number){switch(x){case 0:while(true){break;}b0();break;default:b1();}b2();}
export function thrown(x:number){switch(x){case 0:throw 'SWITCH_SECRET';default:return 1;}}
export function unsafe(x:number){switch(x){case 0:try{return 1;}finally{b0();}}}
'''


def trace(control,source,decision,start=None):
    nodes={n['id']:n for n in control['nodes']}
    current=control['entry'] if start is None else start;steps=[]
    for _ in range(300):
        if current==-1:return steps
        n=nodes[current];at=n['source'];text=source[at['start']:at['start']+at['length']]
        steps.append((n['kind'],text))
        edges=[e for e in control['transfers'] if e['source']==current]
        outcome=decision(n['kind'],text) if len(edges)>1 else edges[0]['outcome']
        current=next(e['target'] for e in edges if e['outcome']==outcome)
    raise AssertionError('trace did not terminate')


def verify_nested(controls,source):
    yes=lambda k,t:'CASE_MATCH'
    for name,expected in [('empty',['d()']),('only',['d()','b0()']),('grouped',['d()','c0()','b0()']),
                          ('nested',['b0()','b2()']),('inside',['b0()','b2()'])]:
        steps=trace(controls[name],source,yes)
        assert [t for k,t in steps if k=='EVALUATE' and t.endswith('()')]==expected,(name,steps)
    c=controls['loop']
    for kind in ('CONTINUE','BREAK'):
        start=next(n['id'] for n in c['nodes'] if n['kind']==kind)
        steps=trace(c,source,lambda k,t:'FALSE',start)
        assert ('EVALUATE','i++') in steps
        assert (('EVALUATE','b0()') in steps)==(kind=='BREAK')
    steps=trace(controls['thrown'],source,yes)
    assert steps[-1][0]=='EXPLICIT_THROW' and not any(k=='RETURN' for k,t in steps)
    c=controls['unsafe']
    assert c['reason']=='STATEMENT_TryStatement' and not c['nodes'] and not c['links']
