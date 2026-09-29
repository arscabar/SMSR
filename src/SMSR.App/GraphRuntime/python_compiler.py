"""Compile without executing targets or exposing constants."""
import dis
import sys
import types
import warnings
from python_bindings import binding, position, slots
from python_control import control
from python_control_dependence import analyze as control_dependence
from python_definitions import analyze as definitions
from python_values import analyze as values
from python_value_summary import analyze as value_summary
from python_local_calls import connect
from python_call_summaries import analyze as call_summaries
from python_keyword_bindings import collect as collect_keywords


def budget(remaining, message):
    def spend(count):
        nonlocal remaining
        remaining -= count
        if remaining < 0:
            raise ValueError(message)
    return spend


def analyze(source, path):
    if sys.implementation.name != 'cpython' or sys.version_info[:2] != (3, 12):
        return dict(status='UNSUPPORTED_RUNTIME', functions=[])
    if len(source.encode('utf-8')) > 2000000:
        raise ValueError('Python source limit')
    try:
        with warnings.catch_warnings():
            warnings.simplefilter('ignore')
            root = compile(source, '<smsr>', 'exec', dont_inherit=True, optimize=0)
    except SyntaxError as error:
        return dict(status='COMPILATION_ERRORS', functions=[],
                    diagnostics=[dict(code=type(error).__name__, line=error.lineno)])
    # splitlines also splits literal Unicode separators that Python does not.
    lines = source.replace('\r\n', '\n').replace('\r', '\n').encode('utf-8').split(b'\n')
    functions = [];call_keywords={}
    work = budget(250000, 'Python dataflow work limit')
    spend = budget(50000, 'Python compiler graph limit')
    module = f'{path}:code:0'
    stack = [(root, module, None, {})]
    while stack:
        code, scope, parent, inherited = stack.pop()
        if len(functions) >= 500:
            raise ValueError('Python scope limit')
        local, cells, free = slots(code, scope, inherited)
        items = list(dis.get_instructions(code, adaptive=False, show_caches=False))
        call_keywords[scope]=collect_keywords(code,items,work)
        spend(len(items) + len(local) + len(cells) + len(free))
        instructions = [dict(offset=i.offset, opcode=i.opname, source=position(i, lines),
                             binding=binding(i, scope, module, local, cells, free)) for i in items]
        edges, regions = control(code, items, spend)
        function = dict(id=scope, parentId=parent, name=code.co_qualname,
            firstLine=code.co_firstlineno, flags=code.co_flags, locals=local, cells=cells, free=free,
            instructions=instructions, edges=edges, exceptionRegions=regions)
        function['definitions'] = definitions(code, function, spend, work)
        function['values'] = values(code, function, items, spend, work)
        function['valueSummary'] = value_summary(code, function, spend, work)
        function['control'] = control_dependence(function, items, spend, work)
        functions.append(function)
        for index, child in reversed(list(enumerate(code.co_consts))):
            if isinstance(child, types.CodeType):
                stack.append((child, f'{scope}/{index}', scope, {**inherited, **cells}))
    connect(functions,spend,work,call_keywords)
    call_summaries(functions,spend,work)
    return dict(status='COMPILER_BYTECODE', runtime=sys.version.split()[0], functions=functions,
                limitations=['CPYTHON_3_12_ONLY', 'NO_RUNTIME_CALL_TARGETS', 'VALUE_CANDIDATES_NOT_TAINT',
                             'EXCEPTION_REGIONS_NOT_EXECUTION_PATHS', 'SUSPENSION_NOT_NORMAL_FALLTHROUGH'])
