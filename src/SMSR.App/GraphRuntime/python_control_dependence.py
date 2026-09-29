"""Control dependence on CPython normal transfers, separate from operand flow."""
import dis
from collections import defaultdict
from control_postdominance import reachable, solve
from python_control import TERMINAL, UNCONDITIONAL, SUSPEND
from python_value_flow import transfers
from python_value_protocols import Unsupported


def analyze(function, items, spend, work):
    result = dict(status='UNAVAILABLE', reason=None, exitNode=-1,
        postdominators=[], links=[], transfers=[], boundaries=[
            'TERMINATING_PATH_POSTDOMINANCE', 'IMPLICIT_EXCEPTIONS_UNMODELED',
            'PATH_CONDITIONS_UNMODELED', 'NOT_TAINT_OR_SAFETY'])
    if any(i.opname in SUSPEND or i.opname == 'SEND' for i in items):
        result['reason'] = 'SUSPENSION'
        return result
    if function['exceptionRegions']:
        result['reason'] = 'EXCEPTION_REGIONS'
        return result
    conditions = {'POP_JUMP_IF_TRUE':'TRUE', 'POP_JUMP_IF_FALSE':'FALSE',
                  'POP_JUMP_IF_NONE':'NONE', 'POP_JUMP_IF_NOT_NONE':'NOT_NONE'}
    inverse = dict(TRUE='FALSE', FALSE='TRUE', NONE='NOT_NONE', NOT_NONE='NONE')
    lookup = {i.offset:i for i in items}
    try:
        edges = transfers(function, items, spend)
    except Unsupported as error:
        result['reason'] = str(error)
        return result
    next_nodes = defaultdict(list)
    for edge in edges:
        op = lookup[edge['source']].opname
        if op in conditions:
            edge['outcome'] = conditions[op] if edge['kind']=='JUMP' else inverse[conditions[op]]
        elif op == 'FOR_ITER':
            edge['outcome'] = edge['kind']
        elif lookup[edge['source']].opcode in dis.hasjabs + dis.hasjrel and op not in UNCONDITIONAL:
            result['reason'] = 'UNSUPPORTED_BRANCH:'+op
            return result
        else:
            edge['outcome'] = 'ALWAYS'
        next_nodes[edge['source']].append(edge['target'])
    live = reachable(items[0].offset, next_nodes, work)
    edges = [e for e in edges if e['source'] in live]
    for offset in sorted(live):
        if not next_nodes[offset]:
            if lookup[offset].opname not in TERMINAL:
                result['reason'] = 'UNKNOWN_TERMINAL'
                return result
            spend(1)
            edges.append(dict(source=offset, target=-1, kind='EXIT', outcome='EXIT'))
    result.update(solve(sorted(live), edges, spend, work))
    if result['status'] != 'UNAVAILABLE':
        result['transfers'] = edges
    return result
