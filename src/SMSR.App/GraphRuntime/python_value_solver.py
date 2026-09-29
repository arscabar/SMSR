"""A bounded fixed point; protocol exits have branch-specific stack states."""
import dis
from collections import deque
from python_value_transfer import transfer
from python_value_protocols import Unsupported
from python_value_flow import iteration


def solve(code, function, items, edges, emit, link, work):
    instructions = {i.offset:i for i in items}
    successors = {}
    for edge in edges:
        successors.setdefault(edge['source'],[]).append(edge)
    entry = items[0].offset
    incoming, pending, queued = {entry:((),0)}, deque([entry]), {entry}
    while pending:
        offset = pending.popleft()
        queued.remove(offset)
        before = incoming[offset]
        work(1 + sum(map(len,before[0])))
        item = instructions[offset]
        outgoing = successors.get(offset,[])
        ordinary = None
        if item.opname != 'FOR_ITER':
            ordinary = transfer(item,before,function,code,emit,link)
            if len(ordinary[0])-len(before[0]) != dis.stack_effect(item.opcode,item.arg):
                raise Unsupported('STACK_EFFECT_MISMATCH')
        for edge in outgoing:
            target = edge['target']
            after = ordinary
            if item.opname == 'FOR_ITER':
                after,effect = iteration(item,before,edge,emit)
                if len(after[0])-len(before[0]) != effect:
                    raise Unsupported('ITERATION_EFFECT_MISMATCH')
            previous = incoming.get(target)
            work(1 + sum(map(len,after[0])) +
                 (sum(map(len,previous[0])) if previous else 0))
            if previous is None:
                combined = after
            else:
                if len(previous[0])!=len(after[0]) or previous[1]!=after[1]:
                    raise Unsupported('STACK_JOIN_MISMATCH')
                combined = tuple(a|b for a,b in zip(previous[0],after[0])),after[1]
            if combined != previous:
                incoming[target] = combined
                if target not in queued:
                    pending.append(target)
                    queued.add(target)
