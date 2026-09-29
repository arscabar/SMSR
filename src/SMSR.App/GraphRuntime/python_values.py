"""Bounded, path-insensitive fixed point over stack producer sets."""
from python_value_graph import build
from python_value_protocols import Unsupported
from python_value_flow import transfers
from python_value_solver import solve


def analyze(code, function, items, spend, work):
    result = dict(status='UNAVAILABLE', reason=None, nodes=[], edges=[], transfers=[], boundaries=[
        'PATH_INSENSITIVE_CANDIDATES', 'NO_HEAP_ALIAS_OR_EXTERNAL_FRAME_MUTATION',
        'OPERATORS_MAY_INVOKE_USER_CODE', 'OPAQUE_CALL_RESULTS', 'NOT_TAINT_OR_SAFETY',
        'ITERATOR_AND_ATTRIBUTE_PROTOCOLS_OPAQUE', 'NORMALIZED_METHOD_RECEIVER_CANDIDATE'])
    reason = function['definitions']['reason']
    if reason:
        pass
    elif any(e['kind']=='RESUME_AFTER_SUSPEND' for e in function['edges']):
        reason = 'SUSPENSION_STACK'
    elif function['exceptionRegions']:
        reason = 'EXCEPTION_STACK_UNWINDING'
    elif function['cells'] or function['free']:
        reason = 'SHARED_CELLS'
    if reason:
        result['reason'] = reason
        return result
    nodes, edges, emit, link = build(function, spend, work)
    try:
        flow = transfers(function,items,spend)
        solve(code,function,items,flow,emit,link,work)
    except Unsupported as error:
        result['reason'] = str(error)
        return result
    result.update(status='VALUE_FLOW_CANDIDATES', nodes=list(nodes.values()), edges=edges, transfers=flow)
    return result
