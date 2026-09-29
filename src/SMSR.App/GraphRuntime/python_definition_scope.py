"""Fast slots only. Shared cells and namespace lookup are separate contracts."""
import inspect


def scope(code, function):
    boundaries = ['NO_HEAP_OR_STACK_VALUES', 'NO_EXTERNAL_FRAME_MUTATION']
    if function['cells'] or function['free']:
        boundaries.append('SHARED_CELLS_NOT_MODELED')
    if function['exceptionRegions']:
        boundaries.append('EXCEPTION_BEFORE_OR_AFTER_CANDIDATES')
    if any(e['kind'] == 'RESUME_AFTER_SUSPEND' for e in function['edges']):
        boundaries.append('RESUMPTION_CANDIDATES')
    reason = None
    if not code.co_flags & inspect.CO_OPTIMIZED:
        reason = 'DYNAMIC_NAMESPACE_SCOPE'
    elif any(i['opcode'] == 'LOAD_FAST_AND_CLEAR' for i in function['instructions']):
        reason = 'STACK_SAVED_SLOT_RESTORATION'
    count = code.co_argcount + code.co_kwonlyargcount
    count += bool(code.co_flags & inspect.CO_VARARGS) + bool(code.co_flags & inspect.CO_VARKEYWORDS)
    return reason, boundaries, set(code.co_varnames[:count])
