"""Independent path enumeration, not the analyzer's definition-set merge."""
import itertools
import random
from test_java_bundle import analyze
from test_typescript_values import reachable


def main():
    rng = random.Random(1702)
    cases, source = [], []
    for index in range(32):
        steps = [(rng.choice(('x','y')), rng.choice(('a','b','x','y','0')),
                  rng.choice(('a','b','x','y','0'))) for _ in range(5)]
        returned = rng.choice(('x','y')); expected = set()
        for choices in itertools.product((False,True),repeat=5):
            values = {'a':{'a'},'b':{'b'},'x':{'a'},'y':{'b'},'0':set()}
            for (target,yes,no),choice in zip(steps,choices):
                values[target] = values[yes if choice else no].copy()
            expected.update(values[returned])
        params = ','.join(f'boolean p{i}' for i in range(5))
        body = ''.join(f'if(p{i}){{{t}={yes};}}else{{{t}={no};}}' for i,(t,yes,no) in enumerate(steps))
        source.append(f'static int case{index}(int a,int b,{params}){{int x=a,y=b;{body}return {returned};}}')
        cases.append(expected)
    result = analyze({'Oracle.java':'class Oracle {\n'+'\n'.join(source)+'\n}'})
    assert result['status']=='BOUND_INPUT_BUNDLE',result['diagnostics']
    assert len(result['methods'])==len(cases)
    for method,expected in zip(result['methods'],cases):
        v=method['values'];assert v['status']=='VALUE_FLOW_CANDIDATES',v
        args=[n for n in v['nodes'] if n['kind']=='PARAMETER_INPUT']
        output=next(n for n in v['nodes'] if n['kind']=='RETURN')
        actual={name for name,node in zip(('a','b'),args) if reachable(v,node['id'],output['id'])}
        assert actual==expected,(method['id'],expected,actual)
    print('Java 32 methods x 32 independent branch paths passed')


if __name__ == '__main__':
    main()
