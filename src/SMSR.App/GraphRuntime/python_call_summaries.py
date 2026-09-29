"""Compose body data dependencies per call, without sharing caller inputs."""
from python_summary_labels import create, spread, add
from python_call_summary_output import update


def analyze(functions, spend, work):
    states={};connections=[]
    for f in functions:
        s=f['valueSummary'];s['callEdges']=[]
        if s['status']!='DATA_DEPENDENCY_CANDIDATES':continue
        resolved=set()
        for c in s['calls']:
            work(1);target=c.get('localTarget',{})
            if target.get('status')=='LOCAL_BODY_CONNECTION_CANDIDATE':
                connections.append((f['id'],c,target));resolved.add(c['resultValueId'])
        states[f['id']]=dict(f=f,labels=create(f['values'],s['parameters'],resolved,spend,work),edges={})
    # ponytail: bounded whole-file fixed point; worklist if profiling justifies it.
    changed=True
    while changed:
        changed=False
        for s in states.values():
            changed=spread(s['f']['values'],s['labels'],spend,work) or changed
        for caller_id,call,target in connections:
            caller=states[caller_id];body=states[target['bodyIds'][0]]
            destination=caller['labels'][call['resultValueId']]
            inputs={a['parameterIndex']:a['argumentValueId'] for a in target['arguments']}
            work(1+len(inputs))
            for r in target['returns']:
                work(1)
                for label in body['labels'][r['returnValueId']]:
                    work(1)
                    if label.startswith('p:'):
                        index=int(label[2:]);source=inputs[index]
                        changed=add(destination,caller['labels'][source],spend,work) or changed
                        key=(call['offset'],index)
                        if key not in caller['edges']:
                            spend(1)
                            caller['edges'][key]=dict(source=source,target=call['resultValueId'],
                                bodyId=target['bodyIds'][0],parameterIndex=index,
                                relation='BODY_DATA_DEPENDENCY_CANDIDATE')
                    else:changed=add(destination,{label},spend,work) or changed
    for s in states.values():update(s,spend,work)
