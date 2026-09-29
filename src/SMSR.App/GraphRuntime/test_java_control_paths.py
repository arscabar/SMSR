"""Trace the emitted normal graph against manually specified Boolean outcomes."""
from itertools import product
from java_control import enrich


def walk(control, fragment, values, start=None):
    lookup={n['id']:n for n in control['nodes']};current=control['entry'] if start is None else start
    trace=[]
    for _ in range(200):
        if current==-1:return trace
        n=lookup[current];text=fragment(n);trace.append((n['kind'],text))
        edges=[e for e in control['transfers'] if e['source']==current]
        if len(edges)==1:current=edges[0]['target']
        else:
            outcome='TRUE' if values[text] else 'FALSE'
            current=next(e['target'] for e in edges if e['outcome']==outcome)
    raise AssertionError('Trace failed to terminate')


def verify(methods,fragment):
    for p,q in product((False,True),repeat=2):
        trace=walk(methods['select'],fragment,dict(p=p,q=q))
        tests=[t for k,t in trace if k=='CONDITION']
        assert tests==(['p','q'] if p else ['p'])
        reads=[t for k,t in trace if k=='EVALUATE' and t in ('a','b')]
        assert reads==(['a'] if p and q else ['b'])
        trace=walk(methods['either'],fragment,dict(p=p,q=q))
        assert [t for k,t in trace if k=='CONDITION']==(['p','q'] if p else ['p'])
        trace=walk(methods['choice'],fragment,dict(p=p))
        assert [t for k,t in trace if k=='EVALUATE' and t in ('a','b')]==(['a'] if p else ['b'])
    for method,update in (('counted','i++'),('once','a++')):
        c=methods[method];start=next(n['id'] for n in c['nodes'] if n['kind']=='CONTINUE')
        trace=walk(c,fragment,{'i<n':False,'p':False},start)
        if method=='counted':assert ('EVALUATE',update) in trace
        else:assert ('EVALUATE',update) not in trace
        assert any(k=='CONDITION' for k,t in trace)
    # Output-limit failure must not return a partial success report.
    huge=dict(status='NORMAL_CFG',entry=0,nodes=[dict(id=0)],
              transfers=[dict(source=0,target=-1,outcome='ALWAYS')]*250001)
    try:
        enrich(dict(methods=[dict(control=huge)]))
        raise AssertionError('Control work budget not enforced')
    except ValueError as error:assert str(error)=='Java control work limit'
