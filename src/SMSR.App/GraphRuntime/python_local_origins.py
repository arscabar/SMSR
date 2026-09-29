"""Function identity moves only through copies, never operations or containers."""


def resolve(model, spend, work):
    copies={'READ','WRITE','CALL_TARGET','FUNCTION_OBJECT'}
    nodes={n['id']:n for n in model['nodes']}
    labels={}
    for n in nodes.values():
        work(1)
        initial={n['bodyId']} if n['kind']=='FUNCTION_CODE' else set() if n['kind'] in copies else {'?'}
        spend(1+len(initial));labels[n['id']]=initial
    edges=[]
    for e in model['edges']:
        work(1)
        target=nodes[e['target']]
        if target['kind'] not in copies:continue
        if target['kind']=='FUNCTION_OBJECT' and e.get('operand')!=0:continue
        edges.append(e)
    # ponytail: bounded fixed point; use a worklist if profiles justify it.
    changed=True
    while changed:
        changed=False
        for e in edges:
            source=labels[e['source']];work(1+len(source))
            added=source-labels[e['target']]
            if added:
                spend(len(added));labels[e['target']].update(added);changed=True
    return labels
