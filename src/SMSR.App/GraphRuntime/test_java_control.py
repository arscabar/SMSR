"""Compiler AST transfers, independently specified branch and evaluation oracles."""
import json
from itertools import product
from test_java_bundle import analyze

SOURCE = '''class Control {
static int early(boolean p,int a,int b){if(p)return a;return b;}
static int join(boolean p,int a){if(p)a++;else a--;return a;}
static int select(boolean p,boolean q,int a,int b){return p && q ? a : b;}
static boolean either(boolean p,boolean q){return !p || q;}
static int counted(int n,boolean p){for(int i=0;i<n;i++){if(p)continue;break;}return n;}
static int once(boolean p,int a){do{a++;continue;}while(p);return a;}
static void endless(){while(true){}}
static void exception(){try{throw new RuntimeException();}catch(Exception e){}}
static void raised(){throw new RuntimeException("CONTROL_SECRET_MARKER");}
static int nested(int a){for(;;){for(;;){break;}break;}return a;}
static int choice(boolean p,int a,int b){return p ? a : b;}
static int array(int[] a,int i){return a[i];}
}
'''


def fragment(node):
    s=node['source'];line=SOURCE.splitlines()[s['start']['line']]
    return line[s['start']['character']:s['end']['character']]


def main():
    result=analyze({'Control.java':SOURCE})
    assert result['status']=='BOUND_INPUT_BUNDLE',result['diagnostics']
    methods={m['id'].split('#')[1].split('(')[0]:m['control'] for m in result['methods']}
    assert result==analyze({'Control.java':SOURCE})
    assert 'CONTROL_SECRET_MARKER' not in json.dumps(result)
    for name,c in methods.items():
        if name in ('endless','exception'):continue
        assert c['status']=='NORMAL_CFG_CONTROL_DEPENDENCE',(name,c)
        ids={n['id'] for n in c['nodes']}|{-1}
        assert all(e['source'] in ids and e['target'] in ids for e in c['transfers'])
    early=methods['early'];nodes={n['id']:n for n in early['nodes']}
    links={(e['outcome'],fragment(nodes[e['dependent']])) for e in early['links']}
    assert ('TRUE','return a;') in links and ('FALSE','return b;') in links
    join=methods['join'];returns={n['id'] for n in join['nodes'] if n['kind']=='RETURN'}
    assert not any(e['dependent'] in returns for e in join['links'])
    assert methods['endless']['reason']=='NON_EXIT_REACHABLE_REGION'
    assert not methods['endless']['links']
    assert methods['exception']['reason']=='STATEMENT_TRY' and not methods['exception']['nodes']
    from test_java_control_paths import verify
    verify(methods,fragment)
    from test_control_postdominance import verify as oracle
    oracle()
    print('Java normal CFG/control, expression order and unsupported boundaries passed')


if __name__=='__main__':main()
