"""Linear case-list oracle independent of analyzer CFG/joins; no Java execution."""
from itertools import product
from test_java_bundle import analyze

def trace(c,line,key):
    nodes={n['id']:n for n in c['nodes']};at=c['entry'];body=[];yields=selectors=0
    for _ in range(150):
        if at==-1:return body,yields,selectors
        n=nodes[at];p=n['source'];text=line[p['start']['character']:p['end']['character']]
        if n['kind']=='EVALUATE' and text.startswith('x='):body.append(text)
        if n['kind'] in ('YIELD','SWITCH_ARM_VALUE'):yields+=1
        if n['kind']=='SWITCH_VALUE':selectors+=1
        edges=[e for e in c['transfers'] if e['source']==at]
        if n['kind']=='CASE_TEST':
            outcome='CASE_MATCH' if int(text)==key else 'CASE_NO_MATCH'
            at=next(e['target'] for e in edges if e['outcome']==outcome)
        else:
            assert len(edges)==1,(text,edges)
            at=edges[0]['target']
    raise AssertionError('Switch expression path failed to terminate')

def verify():
    lines=[];cases=[]
    for default in range(3):
        labels=[0,1];labels.insert(default,None)
        for values in (['p0','p1','0'],['0','p1','p2']):
            for rules,ends in [(False,(*bits,True)) for bits in product((False,True),repeat=2)]+[(True,(True,)*3)]:
                parts=[]
                for label,value,stop in zip(labels,values,ends):
                    head='default' if label is None else f'case {label}'
                    parts.append(head+(f' -> (x={value});' if rules else f':x={value};'+('yield x;' if stop else '')))
                lines.append(f'static int f{len(lines)}(int k,int p0,int p1,int p2)'
                             +'{int x=0;int y=switch(k){'+''.join(parts)+'};return y+x;}')
                cases.append((labels,values,ends))
    r=analyze({'Paths.java':'class Paths {\n'+'\n'.join(lines)+'\n}'})
    assert r['status']=='BOUND_INPUT_BUNDLE',r['diagnostics']
    total=0
    for m,line,(labels,values,ends) in zip(r['methods'],lines,cases):
        expected=set()
        for key in (0,1,9):
            start=labels.index(key) if key in labels else labels.index(None);body=[]
            for i in range(start,3):
                body.append('x='+values[i])
                if ends[i]:break
            assert trace(m['control'],line,key)==(body,1,1),(line,key,body)
            if values[i]!='0':expected.add(int(values[i][-1])+1)
            total+=1
        assert {i for o in m['valueSummary']['returns'] for i in o['parameterIndices']}==expected,line
    assert len(lines)==30 and total==90
