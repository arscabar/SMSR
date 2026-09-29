"""Independent linear case-list oracle, compared with CFG and data summaries."""
from itertools import product
from test_java_bundle import analyze


def trace(c,line,key):
    nodes={n['id']:n for n in c['nodes']};at=c['entry'];body=[];selectors=0
    for _ in range(150):
        if at==-1:return body,selectors
        n=nodes[at];p=n['source'];text=line[p['start']['character']:p['end']['character']]
        if n['kind']=='EVALUATE' and text.startswith('x='):body.append(text)
        if n['kind']=='SWITCH_VALUE':selectors+=1
        edges=[e for e in c['transfers'] if e['source']==at]
        if n['kind']=='CASE_TEST':
            outcome='CASE_MATCH' if int(text)==key else 'CASE_NO_MATCH'
            at=next(e['target'] for e in edges if e['outcome']==outcome)
        else:
            assert len(edges)==1,(text,edges)
            at=edges[0]['target']
    raise AssertionError('Nonterminating switch oracle')


def verify():
    lines=[];cases=[]
    for default in (-1,0,1,2):
        labels=[0,1]
        if default>=0:labels.insert(default,None)
        for breaks in product((False,True),repeat=len(labels)):
            parts=[]
            for i,label in enumerate(labels):
                parts.append(('default:' if label is None else f'case {label}:')+
                             f'x=p{i};'+('break;' if breaks[i] else ''))
            lines.append(f'static int f{len(lines)}(int k,int p0,int p1,int p2)'
                         +'{int x=0;switch(k){'+''.join(parts)+'}return x;}')
            cases.append((labels,breaks))
    source='class Paths {\n'+'\n'.join(lines)+'\n}'
    r=analyze({'Paths.java':source});assert r['status']=='BOUND_INPUT_BUNDLE',r['diagnostics']
    total=0
    for m,line,(labels,breaks) in zip(r['methods'],lines,cases):
        expected=set()
        for key in (0,1,9):
            start=labels.index(key) if key in labels else labels.index(None) if None in labels else len(labels)
            body=[]
            for i in range(start,len(labels)):
                body.append(f'x=p{i}')
                if breaks[i]:break
            assert trace(m['control'],line,key)==(body,1),(line,key,body)
            if body:expected.add(int(body[-1][-1])+1)
            total+=1
        actual={p for out in m['valueSummary']['returns'] for p in out['parameterIndices']}
        assert actual==expected,(line,actual,expected)
    assert len(lines)==28 and total==84
