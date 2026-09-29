"""Encoded CPython 3.12 transfers. Exception regions are candidates, not paths."""
import dis

TERMINAL = {'RETURN_VALUE', 'RETURN_CONST', 'RAISE_VARARGS', 'RERAISE'}
UNCONDITIONAL = {'JUMP_FORWARD', 'JUMP_BACKWARD', 'JUMP_BACKWARD_NO_INTERRUPT'}
SUSPEND = {'YIELD_VALUE', 'RETURN_GENERATOR'}


def control(code, instructions, spend):
    offsets = {i.offset for i in instructions}
    edges = []
    for index, item in enumerate(instructions):
        following = instructions[index+1].offset if index+1 < len(instructions) else None
        if item.opcode in dis.hasjabs + dis.hasjrel:
            if item.argval not in offsets:
                raise ValueError('Unknown Python branch target')
            edges.append(dict(source=item.offset, target=item.argval, kind='JUMP'))
        if following is not None and item.opname not in TERMINAL | UNCONDITIONAL:
            kind = 'RESUME_AFTER_SUSPEND' if item.opname in SUSPEND else 'NEXT'
            edges.append(dict(source=item.offset, target=following, kind=kind))
    regions = [dict(start=e.start, end=e.end, target=e.target, depth=e.depth,
                    lastInstruction=e.lasti, kind='EXCEPTION_REGION')
               for e in dis.Bytecode(code).exception_entries]
    spend(len(edges) + len(regions))
    return edges, regions
