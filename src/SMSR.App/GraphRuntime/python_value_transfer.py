"""CPython 3.12 fast-slot stack transfer. Unsupported semantics fail closed."""
from python_control import UNCONDITIONAL
import types
from python_value_protocols import NULL, RECEIVER, Unsupported, protocol


def transfer(item, state, function, code, emit, link):
    stack, keywords = list(state[0]), state[1]
    op, arg = item.opname, item.arg

    def pop(count):
        if count > len(stack):
            raise Unsupported('STACK_UNDERFLOW')
        result = stack[-count:] if count else []
        if count:
            del stack[-count:]
        if any('<call-null>' in value or RECEIVER in value for value in result):
            raise Unsupported('NULL_VALUE_OPERAND')
        return result

    if op in UNCONDITIONAL | {'RESUME', 'NOP', 'EXTENDED_ARG', 'DELETE_FAST'}:
        pass
    elif op == 'PUSH_NULL':
        stack.append(NULL)
    elif op == 'LOAD_GLOBAL':
        if arg & 1:
            stack.append(NULL)
        stack.append(emit(item, 'EXTERNAL_LOOKUP'))
    elif op in {'LOAD_FAST', 'LOAD_FAST_CHECK'}:
        stack.append(frozenset([f"{function['locals'][item.argval]}:{item.offset}:read"]))
    elif op == 'STORE_FAST':
        link(pop(1)[0], f"{function['locals'][item.argval]}:{item.offset}", 'ASSIGNMENT_INPUT')
    elif op == 'LOAD_CONST':
        if isinstance(item.argval, types.CodeType):
            stack.append(emit(item, 'FUNCTION_CODE', bodyId=f"{function['id']}/{arg}"))
        else:
            stack.append(emit(item, 'CONSTANT_REDACTED'))
    elif op == 'MAKE_FUNCTION':
        if arg & ~15:
            raise Unsupported('FUNCTION_FLAGS')
        body=pop(1)
        configuration=pop(arg.bit_count())
        stack.append(emit(item, 'FUNCTION_OBJECT', body+configuration, flags=arg))
    elif op == 'COPY':
        stack.append(stack[-arg])
    elif op == 'SWAP':
        stack[-1], stack[-arg] = stack[-arg], stack[-1]
    elif op == 'RETURN_CONST':
        emit(item, 'RETURN', [emit(item, 'CONSTANT_REDACTED')])
    elif op == 'RETURN_VALUE':
        emit(item, 'RETURN', pop(1))
    elif op.startswith('POP_JUMP_'):
        emit(item, 'BRANCH_TEST', pop(1))
    elif op == 'POP_TOP':
        pop(1)
    elif op in {'BINARY_OP', 'COMPARE_OP', 'IS_OP', 'CONTAINS_OP', 'BINARY_SUBSCR'}:
        stack.append(emit(item, 'OPERATION', pop(2)))
    elif op in {'UNARY_NEGATIVE', 'UNARY_NOT', 'UNARY_INVERT'}:
        stack.append(emit(item, 'OPERATION', pop(1)))
    elif op in {'BUILD_TUPLE', 'BUILD_LIST', 'BUILD_SET', 'BUILD_SLICE', 'BUILD_STRING'}:
        stack.append(emit(item, 'CONSTRUCTION', pop(arg)))
    else:
        keywords = protocol(item,stack,pop,emit,keywords,code)
    return tuple(stack), keywords
