"""javac-bound local data-flow oracles; target code is never executed."""
from test_java_bundle import analyze
from test_typescript_values import reachable


def main():
    source = '''class Values {
static int identity(int a){return a;}
static int kill(int a){a=0;return a;}
static int choose(int a,boolean p){int x=0;if(p)x=a;return x;}
static int loop(int a,boolean p){int x=0;while(p){x=a;p=false;}return x;}
static int continued(int a){int x=0;for(int i=0;i<1;x=a){i++;continue;}return x;}
static int broken(int a){int x=0;for(;;x=a){break;}return x;}
static int once(int a){int x=a;do{x=0;}while(false);return x;}
static int nested(int a){int x=0;for(;;){for(;;){break;}x=a;break;}return x;}
static int prefix(int a){return ++a;}
static int postfix(int a){return a++;}
static int compound(int a,int b){a+=b;return a;}
static int opaque(int a){return Math.abs(a);}
static int scope(int a){if(a>0){int x=a;x=0;}int x=0;return x;}
static int branchKill(int a,boolean p){int x=a;if(p)x=0;else x=1;return x;}
static int finallyCase(int a){try{return a;}finally{a=0;}}
static int field;static int fieldWrite(int a){field=a;return a;}
static int array(int[] a){return a[0];}
static int lambda(int a){java.util.function.IntSupplier f=()->a;return f.getAsInt();}
}
'''
    result = analyze({'Values.java': source})
    assert result['status'] == 'BOUND_INPUT_BUNDLE', result['diagnostics']
    expected = [True,False,True,True,True,False,False,True,True,True,True,False,False,False]
    methods = result['methods']
    assert len(methods) == 18, [m['id'] for m in methods]
    for m, wanted in zip(methods, expected):
        v = m['values']; assert v['status'] == 'VALUE_FLOW_CANDIDATES', m
        arg = next(n for n in v['nodes'] if n['kind'] == 'PARAMETER_INPUT')
        out = next(n for n in v['nodes'] if n['kind'] == 'RETURN')
        assert reachable(v,arg['id'],out['id']) == wanted, m['id']
        ids = {n['id'] for n in v['nodes']}
        assert all(e['source'] in ids and e['target'] in ids for e in v['edges'])
    for m in methods[14:]:
        assert m['values']['status'] == 'UNAVAILABLE' and not m['values']['nodes'], m
    for m, kind in zip(methods[8:10], ('UPDATE_NEXT_VALUE','UPDATE_PRIOR_VALUE')):
        v=m['values']; edge=next(e for e in v['edges'] if e['relation']=='RETURN_VALUE')
        assert next(n for n in v['nodes'] if n['id']==edge['source'])['kind']==kind
    invalid = analyze({'Bad.java':'class Bad {int f(int x){return missing(x);}}'})
    assert all(m['values']['status']=='UNAVAILABLE' for m in invalid['methods'])
    print('Java local definitions/joins/loops/update order and unavailable boundaries passed')


if __name__ == '__main__':
    main()
