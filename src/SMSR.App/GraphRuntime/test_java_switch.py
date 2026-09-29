"""Java17 switch statements: actual attribution, no target execution."""
import json
from test_java_bundle import analyze

SOURCE='''class Switches {
enum E { A,B }
static int pick(int k,int a,int b){switch(k){case 0:return a;default:return b;}}
static int fall(int k,int a,int b){int x=0;switch(k){case 0:x=a;default:x=b;case 1:x=0;}return x;}
static int rule(int k,int a,int b){int x=0;switch(k){case 0,1 -> x=a;default -> {x=b;}}return x;}
static int grouped(int k,int a){int x=0;switch(k){case 0:case 1:x=a;break;}return x;}
static int text(String k,int a){switch(k){case "PRIVATE_CASE_7163":return a;default:return 0;}}
static int enumeration(E k,int a){switch(k){case A:return a;case B:break;}return 0;}
static int continued(int k,int a){int x=0;for(int i=0;i<1;x=a){i++;switch(k){case 0:continue;default:break;}x=0;}return x;}
static int nested(int k,int a){int x=0;switch(k){case 0:switch(k){default:x=a;break;}break;default:break;}return x;}
static int selector(int k,int a){int x=0;switch(x=a){default:break;}return x;}
static int empty(int k,int a){switch(k){}return a;}
static int earlyRule(int k,int a){switch(k){case 0 -> {return a;}default -> {return 0;}}}
static int expression(int k){return switch(k){default -> 0;};}
}'''


def main():
    r=analyze({'Switches.java':SOURCE})
    assert r['status']=='BOUND_INPUT_BUNDLE',r['diagnostics']
    assert r==analyze({'Switches.java':SOURCE})
    assert 'PRIVATE_CASE_7163' not in json.dumps(r)
    expected=[[1,2],[],[1,2],[1],[1],[1],[1],[1],[1],[1],[1],[]]
    assert len(r['methods'])==len(expected)
    for m,want in zip(r['methods'],expected):
        c=m['control'];s=m['valueSummary']
        if want is None:
            assert c['status']==s['status']=='UNAVAILABLE';continue
        assert c['status']=='NORMAL_CFG_CONTROL_DEPENDENCE',m
        assert sorted({p for v in s['returns'] for p in v['parameterIndices']})==want,m
        assert not any(v['unknownValueIds'] for v in s['returns']),m
    from test_java_switch_paths import verify
    verify()
    from test_java_switch_boundaries import verify
    verify()
    print('Java switch colon/arrow, multi-label, enum/string, nesting and 96 paths passed')


if __name__=='__main__':main()
