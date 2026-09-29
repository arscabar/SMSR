"""Opaque protocol operations; method stacks use normalized receiver slots."""
NULL = frozenset(['<call-null>'])
RECEIVER = '<method-receiver>'


class Unsupported(Exception):
    pass


def protocol(item, stack, pop, emit, keywords, code):
    op, arg = item.opname, item.arg
    if op == 'GET_ITER':
        stack.append(emit(item, 'ITERATOR', pop(1)))
    elif op == 'LOAD_ATTR':
        owner = pop(1)
        lookup = emit(item, 'ATTRIBUTE_LOOKUP', owner, name=item.argval)
        if arg & 1:
            stack.append(owner[0] | {RECEIVER})
        stack.append(lookup)
    elif op == 'UNPACK_SEQUENCE':
        value = pop(1)
        parts = [emit(item, 'UNPACKED_ITEM', value, str(i), itemIndex=i) for i in range(arg)]
        stack.extend(reversed(parts))
    elif op == 'KW_NAMES':
        keywords = len(code.co_consts[arg])
    elif op == 'CALL':
        arguments, target = pop(arg), pop(1)
        if not stack or keywords > arg:
            raise Unsupported('CALL_LAYOUT_MISMATCH')
        prefix = stack.pop()
        if prefix != NULL:
            if RECEIVER not in prefix:
                raise Unsupported('CALL_LAYOUT_MISMATCH')
            emit(item, 'CALL_RECEIVER_CANDIDATE', [prefix - {RECEIVER, '<call-null>'}])
        emit(item, 'CALL_TARGET', target)
        for index, value in enumerate(arguments):
            emit(item, 'CALL_ARGUMENT', [value], str(index), argument=index,
                 keyword=index >= arg-keywords)
        stack.append(emit(item, 'OPAQUE_CALL_RESULT'))
        keywords = 0
    else:
        raise Unsupported('UNSUPPORTED_OPCODE:'+op)
    return keywords
