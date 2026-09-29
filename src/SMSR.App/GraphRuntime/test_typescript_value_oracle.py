"""Enumerate independent branch choices without running analyzed JavaScript."""
import itertools
import random
from typescript_bundle import run
from test_typescript_values import reachable


def main():
    rng = random.Random(8103)
    cases, source = [], []
    for index in range(32):
        steps = [(rng.choice(('x', 'y')), rng.choice(('a', 'b', 'x', 'y', '0')),
                  rng.choice(('a', 'b', 'x', 'y', '0'))) for _ in range(5)]
        returned = rng.choice(('x', 'y'))
        expected = set()
        for choices in itertools.product((False, True), repeat=5):
            values = {'a': {'a'}, 'b': {'b'}, 'x': {'a'}, 'y': {'b'}, '0': set()}
            for (target, yes, no), choice in zip(steps, choices):
                values[target] = values[yes if choice else no].copy()
            expected.update(values[returned])
        params = ','.join(f'p{i}:boolean' for i in range(5))
        body = ''.join(f'if(p{i}){{{t}={yes};}}else{{{t}={no};}}' for i, (t, yes, no) in enumerate(steps))
        source.append(f'function case{index}(a:number,b:number,{params}){{let x=a,y=b;{body}return {returned};}}')
        cases.append(expected)
    result = run({'files': [{'path': 'oracle.ts', 'text': '\n'.join(source)}]})
    assert result['status'] == 'COMPILER_BINDINGS', result['diagnostics']
    assert len(result['functions']) == len(cases)
    for function, expected in zip(result['functions'], cases):
        values = function['values']
        assert values['status'] == 'VALUE_FLOW_CANDIDATES', values
        inputs = [n for n in values['nodes'] if n['kind'] == 'PARAMETER_INPUT']
        target = next(n for n in values['nodes'] if n['kind'] == 'RETURN')
        actual = {name for name, node in zip(('a', 'b'), inputs)
                  if reachable(values, node['id'], target['id'])}
        assert actual == expected, (function['line'], expected, actual)
    print('32 functions x 32 independent branch paths: reaching-value oracle passed')


if __name__ == '__main__':
    main()
