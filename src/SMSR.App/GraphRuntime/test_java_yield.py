"""Actual javac switch expressions; sources are compiled for facts, never run."""
import json
from test_java_bundle import analyze

SOURCE='''class Yields {
enum E { A,B }
static int pick(int k,int a,int b){return switch(k){case 0,1 -> a;default -> b;};}
static int block(int k,int a,int b){return switch(k){case 0 -> {if(a>0)yield a;yield b;}default -> {yield b;}};}
static int fall(int k,int a,int b){int x=0;return switch(k){case 0:x=a;default:x=b;case 1:x=0;yield x;};}
static int nested(int k,int a,int b){return switch(k){case 0 -> {int x=switch(k){default -> a;};yield x;}default -> b;};}
static int statement(int k,int a,int b){return switch(k){default -> {switch(k){case 0:yield a;default:break;}yield b;}};}
static int loop(int k,int a,int b){return switch(k){default -> {for(int i=0;i<2;i++){if(k>0)yield a;continue;}yield b;}};}
static int effects(int k,int a,int b){int x=0;int y=switch(k){case 0 -> {x=a;yield 0;}default -> {x=b;yield 0;}};return x;}
static int erased(int k,int a){int x=switch(k){default -> a;};x=0;return x;}
static int relay(int k,int a,int b){return pick(k,a,b);}
static int killed(int k,int a){return erased(k,a);}
static int enumeration(E k,int a,int b){return switch(k){case A -> a;case B -> b;};}
static int text(String k,int a){return switch(k){case "PRIVATE_YIELD_526" -> a;default -> 0;};}
static boolean condition(int k,boolean a){if(switch(k){case 0 -> a;default -> false;})return a;return false;}
}'''

def verify(report):
    assert report['status']=='BOUND_INPUT_BUNDLE',report['diagnostics']
    expected=[[1,2],[1,2],[],[1,2],[1,2],[1,2],[1,2],[],[1,2],[],[1,2],[1],[1]]
    assert len(report['methods'])==len(expected)
    for m,want in zip(report['methods'],expected):
        assert m['control']['status']=='NORMAL_CFG_CONTROL_DEPENDENCE',m
        s=m['valueSummary'];assert s['status']=='DATA_DEPENDENCY_CANDIDATES',m
        assert sorted({i for out in s['returns'] for i in out['parameterIndices']})==want,m
        assert not any(out['unknownValueIds'] for out in s['returns']),m
    assert len(report['methods'][8]['valueSummary']['callEdges'])==2
    assert not report['methods'][9]['valueSummary']['callEdges']
    assert 'PRIVATE_YIELD_526' not in json.dumps(report)

def main():
    r=analyze({'Yields.java':SOURCE});verify(r)
    assert r==analyze({'Yields.java':SOURCE})
    from test_java_yield_paths import verify as paths
    paths()
    from test_java_yield_boundaries import verify as boundaries
    boundaries()
    print('Java switch expressions/yield: nested states, calls, boundaries and 90 paths passed')

if __name__=='__main__':main()
