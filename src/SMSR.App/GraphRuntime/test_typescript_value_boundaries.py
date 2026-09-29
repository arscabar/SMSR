from typescript_bundle import run


def main():
    source = '''function nested(x:number){function f(){x=0;}f();return x;}
function guarded(x:number){try{return x;}finally{x=0;}}
function property(x:{value:number}){return x.value;}
function increment(x:{value:number}){x.value++;return x;}
function dynamic(x:number){eval('x=0');return x;}
function defaults(x=0){return x;}
function destruct({x}:{x:number}){return x;}
function forloop(x:number[]){for(const value of x){return value;}return 0;}
function args(x:number){return arguments[0];}
'''
    result = run({'files': [{'path': 'boundaries.ts', 'text': source}]})
    assert result['status'] == 'COMPILER_BINDINGS', result['diagnostics']
    top = [f for f in result['functions'] if f['start'] != source.index('function f()')]
    assert len(top) == 9
    for f in top:
        value = f['values']
        assert value['status'] == 'UNAVAILABLE' and value['reason']
        assert not value['nodes'] and not value['edges']
    # Compiler errors do not become successful partial value graphs.
    bad = run({'files': [{'path': 'bad.ts', 'text': 'function f(x:number){let y:string=x;return y;}'}]})
    assert bad['functions'][0]['values']['reason'] == 'COMPILATION_ERRORS'
    print('TypeScript unsupported-function and compiler-error boundaries passed')


if __name__ == '__main__':
    main()
