"""Enumerated dispatch/body states are independent of the value graph solver."""
from typescript_bundle import run
from test_typescript_values import reachable


def main():
    checked=0
    for writes in range(8):
        source='';cases=[]
        for default in range(-1,3):
            order=[0,1]
            if default>=0:order.insert(default,2)
            for breaks in range(1<<len(order)):
                name=f'f{len(cases)}';parts=[]
                for pos,i in enumerate(order):
                    label=['case (y=b)','case (y=c)','default'][i]
                    body=['y=a;','y=0;','y=b;'][i] if writes&(1<<i) else ''
                    parts.append(label+':'+body+('break;' if breaks&(1<<pos) else ''))
                source+=f'function {name}(mode:number,a:number,b:number,c:number){{let y=a;switch(mode){{'+''.join(parts)+'}return y;}\n'
                wanted=set()
                for selected in (0,1,None):
                    value=2 if selected==0 else 3
                    start=selected if selected is not None else 2 if 2 in order else None
                    if start is not None:
                        for pos in range(order.index(start),len(order)):
                            i=order[pos]
                            if writes&(1<<i):value=[1,None,2][i]
                            if breaks&(1<<pos):break
                    if value is not None:wanted.add(value)
                    checked+=1
                cases.append((name,wanted))
        r=run(dict(files=[dict(path='switch_values.ts',text=source)]))
        assert r['status']=='COMPILER_BINDINGS',r['diagnostics']
        functions={f['name']:f for f in r['functions']}
        for name,wanted in cases:
            f=functions[name];v=f['values'];assert v['status']=='VALUE_FLOW_CANDIDATES',v
            args=[n for n in v['nodes'] if n['kind']=='PARAMETER_INPUT']
            output=next(n for n in v['nodes'] if n['kind']=='RETURN')
            actual={i for i,a in enumerate(args) if reachable(v,a['id'],output['id'])}
            assert actual==wanted,(writes,name,actual,wanted)
            s=f['valueSummary'];assert set(s['returns'][0]['parameterIndices'])==wanted
            assert not s['returns'][0]['unknownValueIds']
            ids={n['id'] for n in v['nodes']}
            assert all(e['source'] in ids and e['target'] in ids for e in v['edges'])
    from test_typescript_switch_value_paths import verify
    verify()
    print(f'JS/TS switch values: 224 layouts, {checked} independent paths and boundary summaries passed')


if __name__=='__main__':main()
