"""Data-dependence oracles distinguish overwrites, joins and opaque calls."""
from typescript_bundle import run


def reachable(values, source, target):
    seen, todo = {source}, [source]
    while todo:
        current = todo.pop()
        for edge in values['edges']:
            if edge['relation'] == 'CONTROL_CONDITION_CANDIDATE':
                continue
            if edge['source'] == current and edge['target'] not in seen:
                seen.add(edge['target'])
                todo.append(edge['target'])
    return target in seen


def main():
    source = '''declare function opaque(x:number):number;
function identity(x:number){return x;}
function kill(x:number){x=0;return x;}
function choose(x:number,p:boolean){let y=0;if(p){y=x;}else{y=1;}return y;}
function loop(x:number,p:boolean){let y=0;while(p){y=x;p=false;}return y;}
function call(x:number){let y=opaque(x);return y;}
function shadow(x:number){if(x){let x=0;x=1;}return x;}
function early(x:number,p:boolean){if(p){return 0;}x=0;return x;}
function hoist(x:number){var x:number;return x;}
function short(x:number,p:boolean){let y=0;p&&(y=x);return y;}
function bound(x:number){return identity(x);}
'''
    result = run({'files': [{'path': 'values.ts', 'text': source}]})
    assert result['status'] == 'COMPILER_BINDINGS', result['diagnostics']
    reports = result['functions']
    for f, expected in zip(reports, [True, False, True, True, False, True, False, True, True, False]):
        v = f['values']
        assert v['status'] == 'VALUE_FLOW_CANDIDATES', v
        inputs = [n for n in v['nodes'] if n['kind'] == 'PARAMETER_INPUT']
        returns = [n for n in v['nodes'] if n['kind'] == 'RETURN']
        assert any(reachable(v, inputs[0]['id'], r['id']) for r in returns) == expected, f['line']
        ids = {n['id'] for n in v['nodes']}
        assert all(e['source'] in ids and e['target'] in ids for e in v['edges'])
    call = reports[4]['values']
    arg = next(n for n in call['nodes'] if n['kind'] == 'CALL_ARGUMENT_0')
    parameter = next(n for n in call['nodes'] if n['kind'] == 'PARAMETER_INPUT')
    assert reachable(call, parameter['id'], arg['id'])
    loop = reports[3]['values']
    read = max((n for n in loop['nodes'] if n['kind'] == 'LOCAL_READ'), key=lambda n: n['start'])
    assert sum(e['target'] == read['id'] and e['relation'] == 'REACHING_DEFINITION' for e in loop['edges']) == 2
    assert run({'files': [{'path': 'values.ts', 'text': source}]}) == result
    connection = next(c for c in result['connections'] if c['bodyId'] == reports[0]['id'])
    assert connection['inputs'][0]['parameterValueId'] and connection['inputs'][0]['argumentValueId']
    assert connection['returns'][0]['returnValueId'] and connection['returns'][0]['callValueId']
    print('TypeScript GEN/KILL/join/loop/call-boundary value checks passed')


if __name__ == '__main__':
    main()
