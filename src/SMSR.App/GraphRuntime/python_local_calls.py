"""Local function-object evidence; no global name matching or runtime dispatch."""
from python_local_origins import resolve
from python_keyword_bindings import bind


def connect(functions, spend, work, keywords=None):
    bodies={f['id']:f for f in functions}
    for f in functions:
        summary=f['valueSummary']
        if not summary['calls']:continue
        origins=resolve(f['values'],spend,work)
        for call in summary['calls']:
            work(1)
            candidates=origins.get(call['target']['valueId'],set()) if call['target'] else set()
            ids=sorted(candidates-{'?'})
            spend(1+len(ids))
            result=dict(status='UNRESOLVED',bodyIds=ids,hasUnknown='?' in candidates,
                        reason='NO_UNIQUE_LOCAL_FUNCTION',arguments=[],returns=[])
            call['localTarget']=result
            if len(ids)!=1 or '?' in candidates:
                if ids:result['status']='AMBIGUOUS'
                continue
            result.update(status='LOCAL_FUNCTION_CANDIDATE',reason=None)
            body=bodies.get(ids[0]);target=body['valueSummary'] if body else None
            if not target or target['status']!='DATA_DEPENDENCY_CANDIDATES':
                result['reason']='BODY_VALUES_UNAVAILABLE';continue
            parameters=target['parameters'];args=call['arguments']
            names=(keywords or {}).get(f['id'],{}).get(call['offset'],None if any(a['keyword'] for a in args) else ())
            bindings,reason=bind(parameters,args,names,spend,work)
            if call['receiver'] or reason:
                result.update(reason='ARGUMENT_BINDING_UNSUPPORTED',bindingReason=reason or 'METHOD_RECEIVER');continue
            result['arguments']=bindings
            for ret in target['returns']:
                spend(1)
                result['returns'].append(dict(returnValueId=ret['valueId'],resultValueId=call['resultValueId']))
            result['status']='LOCAL_BODY_CONNECTION_CANDIDATE'
