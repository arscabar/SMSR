"""Independent switch selection/body oracle; never executes analyzed code."""
import json
from typescript_bundle import run
from test_typescript_switch_paths import trace, verify_nested, SOURCE as NESTED


def main():
    source='declare function d():number;\n'
    source+=''.join(f'declare function c{i}():number;declare function b{i}():void;\n' for i in range(4))
    cases=[]
    for default in range(-1,4):
        order=list(range(3))
        if default>=0:order.insert(default,3)
        for mask in range(1<<len(order)):
            name=f'f{len(cases)}';parts=[]
            for pos,i in enumerate(order):
                label='default' if i==3 else f'case c{i}()'
                parts.append(f'{label}:b{i}();'+('break;' if mask&(1<<pos) else ''))
            source+=f'export function {name}(){{switch(d()){{'+''.join(parts)+'}}\n'
            cases.append((name,order,mask))
    source+=NESTED
    result=run(dict(files=[dict(path='switch.ts',text=source)]))
    assert result['status']=='COMPILER_BINDINGS',result['diagnostics']
    assert result==run(dict(files=[dict(path='switch.ts',text=source)]))
    assert 'SWITCH_SECRET' not in json.dumps(result)
    controls={f['name']:f['control'] for f in result['functions']}
    checks=0
    for name,order,mask in cases:
        c=controls[name];assert c['status']=='NORMAL_CFG_CONTROL_DEPENDENCE',(name,c)
        for matches in (set(),{0},{1},{2},{0,2}):
            selected=min(matches) if matches else 3 if 3 in order else None
            expected=['d()']+[f'c{i}()' for i in range(min(matches)+1 if matches else 3)]
            if selected is not None:
                for pos in range(order.index(selected),len(order)):
                    expected.append(f'b{order[pos]}()')
                    if mask&(1<<pos):break
            steps=trace(c,source,lambda kind,text: 'CASE_MATCH' if text in {f'c{i}()' for i in matches} else 'CASE_NO_MATCH')
            actual=[text for kind,text in steps if kind=='EVALUATE' and text.endswith('()')]
            assert actual==expected,(name,matches,actual,expected)
            checks+=1
    verify_nested(controls,source)
    js=run(dict(files=[dict(path='switch.js',text='/** @param {number} x */ export function pick(x){switch(x){case 1:return 2;default:return 3;}}')]))
    assert js['status']=='COMPILER_BINDINGS' and js['functions'][0]['control']['links']
    print(f'JS/TS switch: {len(cases)} layouts, {checks} independent traces and nested transfers passed')


if __name__=='__main__':main()
