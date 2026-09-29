"""Normal CPython transfers; FOR_ITER exhaustion bypasses END_FOR."""
import dis
from python_value_protocols import NULL, RECEIVER, Unsupported


def transfers(function, items, spend):
    instructions = {i.offset:i for i in items}
    following = {a.offset:b.offset for a,b in zip(items,items[1:])}
    result = []
    for edge in function['edges']:
        item = instructions[edge['source']]
        edge = dict(edge)
        if item.opname == 'FOR_ITER':
            if edge['kind'] == 'JUMP':
                end = edge['target']
                if instructions[end].opname != 'END_FOR' or end not in following:
                    raise Unsupported('FOR_EXIT_LAYOUT_MISMATCH')
                edge.update(target=following[end], kind='ITERATION_EXHAUSTED', skippedOffset=end)
            else:
                edge['kind'] = 'ITERATION_NEXT'
        spend(1)
        result.append(edge)
    return result


def iteration(item, before, edge, emit):
    stack, keywords = list(before[0]), before[1]
    if not stack or stack[-1] & (NULL | {RECEIVER}):
        raise Unsupported('ITERATOR_STACK_MISMATCH')
    exhausted = edge['kind']=='ITERATION_EXHAUSTED'
    if exhausted:
        emit(item, 'ITERATION_END', [stack.pop()])
    else:
        stack.append(emit(item, 'ITERATION_VALUE', [stack[-1]]))
    effect = dis.stack_effect(item.opcode,item.arg,jump=exhausted)
    if exhausted:
        effect += dis.stack_effect(dis.opmap['END_FOR'])
    return (tuple(stack),keywords), effect
