"""Summarize existing MAY value edges, without inventing call-return edges."""
from python_summary_slots import parameters, calls
from python_summary_labels import create, spread, describe as describe_labels


def analyze(code, function, spend, work):
    model=function['values']
    result=dict(status='UNAVAILABLE',reason=model['reason'],parameters=[],returns=[],calls=[],boundaries=[
        'PATH_INSENSITIVE_DATA_CANDIDATES', 'STATIC_LOCAL_BODIES_NOT_RUNTIME_TARGETS',
        'OPERATORS_AND_PROTOCOLS_MAY_INVOKE_USER_CODE', 'NOT_TAINT_OR_SAFETY'])
    if model['status']!='VALUE_FLOW_CANDIDATES':return result
    slots=parameters(code,function,spend)
    labels=create(model,slots,set(),spend,work)
    # ponytail: bounded fixed point matches existing summaries; worklist if profiles justify it.
    while spread(model,labels,spend,work):pass

    def describe(n):
        return dict(valueId=n['id'],source=n['source'],**describe_labels(labels[n['id']],spend,work))

    result.update(status='DATA_DEPENDENCY_CANDIDATES',parameters=slots,
                  returns=[describe(n) for n in model['nodes'] if n['kind']=='RETURN'],
                  calls=calls(model['nodes'],describe,spend,work))
    return result
