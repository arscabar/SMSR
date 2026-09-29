from itertools import product


def verify(functions,source):
    def text(n):
        at=n['source'];return source[at['start']:at['start']+at['length']]

    def trace(c,values,start=None):
        nodes={n['id']:n for n in c['nodes']};current=c['entry'] if start is None else start
        steps=[]
        for _ in range(200):
            if current==-1:return steps
            n=nodes[current];fragment=text(n);steps.append((n['kind'],fragment))
            edges=[e for e in c['transfers'] if e['source']==current]
            if len(edges)==1:current=edges[0]['target'];continue
            if n['kind']=='NULLISH_TEST':outcome='NULLISH' if values[fragment] is None else 'NON_NULLISH'
            else:outcome='TRUE' if values[fragment] else 'FALSE'
            current=next(e['target'] for e in edges if e['outcome']==outcome)
        raise AssertionError('trace did not terminate')

    early=functions['early'];nodes={n['id']:n for n in early['nodes']}
    links={(e['outcome'],text(nodes[e['dependent']])) for e in early['links']}
    assert ('TRUE','return a;') in links and ('FALSE','return b;') in links
    joined=functions['join'];returns={n['id'] for n in joined['nodes'] if n['kind']=='RETURN'}
    assert not any(e['dependent'] in returns for e in joined['links'])
    for p,q in product((False,True),repeat=2):
        steps=trace(functions['select'],dict(p=p,q=q))
        assert [t for k,t in steps if k=='CONDITION']==(['p','q'] if p else ['p'])
        assert [t for k,t in steps if k=='EVALUATE' and t in ('a','b')]==(['a'] if p and q else ['b'])
    for a in (None,False,True,0,1,''):
        for name in ('coalesce','andSet','orSet','nullSet'):
            steps=trace(functions[name],dict(a=a))
            right=a is None if name in ('coalesce','nullSet') else bool(a) if name=='andSet' else not bool(a)
            assert (('EVALUATE','b') in steps)==right,(name,a,steps)
            assert sum(k=='ASSIGNMENT' for k,t in steps)==int(right and name!='coalesce')
            assert sum(k=='EVALUATE' and t=='a' for k,t in steps)==(1 if name=='coalesce' else 2)
    for name in ('counted','once'):
        c=functions[name];start=next(n['id'] for n in c['nodes'] if n['kind']=='CONTINUE')
        steps=trace(c,{'i<n':False,'p':False},start)
        assert (('EVALUATE','i++') in steps)==(name=='counted')
        assert ('EVALUATE','a++') not in steps
    assert not any(n['kind']=='RETURN' and text(n)=='a' for n in functions['deferred']['nodes'])
