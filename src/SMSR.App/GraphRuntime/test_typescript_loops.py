"""Abrupt loop transfers and update-value order are separate from path solving."""
from typescript_bundle import run
from test_typescript_values import reachable


def main():
    source = '''function continued(a:number){let x=0;for(let i=0;i<1;x=a){i++;continue;}return x;}
function broken(a:number){let x=0;for(;;x=a){break;}return x;}
function once(a:number){let x=a;do{x=0;}while(false);return x;}
function dobreak(a:number){let x=a;do{x=0;break;x=a;}while(true);return x;}
function nested(a:number){let x=0;for(;;){for(;;){break;}x=a;break;}return x;}
function skipped(a:number,p:boolean){let x=0;while(p){p=false;x=a;continue;x=0;}return x;}
function returned(a:number){for(;;){return a;}return 0;}
function prefix(a:number){return ++a;}
function postfix(a:number){return a++;}
function compound(a:number,b:number){a+=b;return a;}
function logical(a:number,b:number){a||=b;return a;}
function initializer(a:number){let i=0;for(i=a;i>0;i--){}return i;}
'''
    result = run({'files': [{'path': 'loops.ts', 'text': source}]})
    assert result['status'] == 'COMPILER_BINDINGS', result['diagnostics']
    functions = result['functions']
    assert len(functions) == 12
    for f, expected in zip(functions, [True, False, False, False, True, True, True, True, True, True, True, True]):
        v = f['values']
        assert v['status'] == 'VALUE_FLOW_CANDIDATES', v
        arg = next(n for n in v['nodes'] if n['kind'] == 'PARAMETER_INPUT')
        returns = [n for n in v['nodes'] if n['kind'] == 'RETURN']
        assert len(returns) == 1
        assert reachable(v, arg['id'], returns[0]['id']) == expected, f['line']
    for f, kind in zip(functions[7:9], ('UPDATE_NEXT_VALUE', 'UPDATE_PRIOR_VALUE')):
        v = f['values']; ids = {n['id']: n for n in v['nodes']}
        edge = next(e for e in v['edges'] if e['relation'] == 'RETURN_VALUE')
        assert ids[edge['source']]['kind'] == kind
    for f in functions[9:11]:
        v = f['values']; args = [n for n in v['nodes'] if n['kind'] == 'PARAMETER_INPUT']
        target = next(n for n in v['nodes'] if n['kind'] == 'RETURN')
        assert all(reachable(v, a['id'], target['id']) for a in args)
    print('TypeScript for/do/break/continue/nesting and update ordering passed')


if __name__ == '__main__':
    main()
