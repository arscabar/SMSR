"""Do not promote virtual/varargs/error dispatch into a resolved value path."""
def verify(analyze):
    files={
        'Lib.java':'''class Lib {
static int pick(int a){return a;}
static String pick(String a){return a;}
int virtual(int a){return a;}
final int direct(int a){return a;}
static int varargs(int... a){return 0;}
}''',
        'Use.java':'''class Use {
static int integer(int a){return Lib.pick(a);}
static String text(String a){return Lib.pick(a);}
static int dynamic(Lib l,int a){return l.virtual(a);}
static int direct(Lib l,int a){return l.direct(a);}
static int spread(int a){return Lib.varargs(a);}
}'''}
    r=analyze(files);assert r['status']=='BOUND_INPUT_BUNDLE'
    cases={m['id'].split('#')[1].split('(')[0]:m for m in r['methods'] if m['id'].startswith('source:Use#')}
    for name,indices,unknown in [('integer',[0],False),('text',[0],False),('dynamic',[],True),('direct',[1],False),('spread',[],True)]:
        s=cases[name]['valueSummary'];out=s['returns'][0]
        assert out['parameterIndices']==indices and bool(out['unknownValueIds'])==unknown,(name,s)
    linked=[c['bodyId'] for c in r['connections'] if c['status']=='STATIC_BODY_CANDIDATE']
    assert 'source:Lib#pick(int)' in linked and 'source:Lib#pick(java.lang.String)' in linked
    reasons={c['reason'] for c in r['connections']}
    assert {'NON_DIRECT_DISPATCH','VARARGS_PROTOCOL'}<=reasons
    invalid=analyze({'Bad.java':'class Bad {int f(int x){return missing(x);}}'})
    assert all(m['valueSummary']['status']=='UNAVAILABLE' for m in invalid['methods'])
    assert all(c['status']=='UNAVAILABLE' for c in invalid['connections'])
