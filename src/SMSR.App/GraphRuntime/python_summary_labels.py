"""Bounded data labels shared by local and call-composed summaries."""
UNKNOWN={'EXTERNAL_LOOKUP','OPAQUE_CALL_RESULT','UNBOUND_ENTRY','UNBOUND_DELETE',
         'ATTRIBUTE_LOOKUP','ITERATOR','ITERATION_VALUE','UNPACKED_ITEM'}


def add(target, source, spend, work):
    work(1+len(source));added=source-target
    if not added:return False
    spend(len(added));target.update(added);return True


def create(model, parameters, resolved, spend, work):
    seeds={p['valueId']:'p:'+str(p['index']) for p in parameters}
    labels={}
    for n in model['nodes']:
        work(1);seed=seeds.get(n['id'])
        initial={seed} if seed else {'u:'+n['id']} if n['kind'] in UNKNOWN and n['id'] not in resolved else set()
        spend(1+len(initial));labels[n['id']]=initial
    return labels


def spread(model, labels, spend, work):
    changed=False
    for e in model['edges']:
        work(1)
        if e['relation']!='CONTROL_CONDITION_CANDIDATE':
            changed=add(labels[e['target']],labels[e['source']],spend,work) or changed
    return changed


def describe(values, spend, work):
    work(len(values));spend(1+len(values))
    return dict(parameterIndices=sorted(int(v[2:]) for v in values if v.startswith('p:')),
                unknownValueIds=sorted(v[2:] for v in values if v.startswith('u:')))
