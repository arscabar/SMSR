from pathlib import PurePosixPath
from tree_sitter_language_pack import get_parser
from symbol_positions import name, selection

LANGUAGES = {'.cs':'csharp', '.py':'python', '.js':'javascript', '.jsx':'javascript', '.ts':'typescript', '.tsx':'tsx', '.java':'java', '.go':'go', '.rs':'rust', '.c':'c', '.h':'c', '.cpp':'cpp', '.hpp':'cpp', '.rb':'ruby', '.php':'php', '.kt':'kotlin', '.swift':'swift', '.scala':'scala', '.lua':'lua'}
DECLARATIONS = {'class_definition', 'class_declaration', 'interface_declaration', 'struct_specifier', 'struct_item', 'enum_item', 'enum_declaration', 'function_definition', 'function_declaration', 'method_declaration', 'method_definition', 'function_item', 'method', 'singleton_method'}
FUNCTIONS = {t for t in DECLARATIONS if any(s in t for s in ('function', 'method'))}
CALLS = {'call', 'call_expression', 'invocation_expression', 'method_invocation', 'function_call_expression', 'function_call', 'member_call_expression', 'scoped_call_expression'}

def walk(node):
    stack = [node]
    while stack:
        current = stack.pop()
        yield current
        stack.extend(reversed(current.named_children))

def run(request):
    path, source = request['path'], request['source']
    language = LANGUAGES.get(PurePosixPath(path).suffix.lower())
    if not language:
        return dict(path=path, status='UNSUPPORTED', supported=LANGUAGES, symbols=[], calls=[], statements=[], edges=[], findings=[])
    raw = source.encode('utf-8')
    if len(raw) > 2000000:
        raise ValueError('Source exceeds 2 MB')
    tree = get_parser(language).parse(raw)
    symbols, calls, functions = [], [], []
    for node in walk(tree.root_node):
        if node.type in DECLARATIONS:
            symbols.append(dict(id=f'{path}:{node.start_byte}', name=name(node), kind=node.type, line=node.start_point.row+1, endLine=node.end_point.row+1, selection=selection(node, raw), resolution='SYNTAX_ONLY'))
            if node.type in FUNCTIONS:
                functions.append(node)
        if node.type in CALLS:
            owner = node.parent
            while owner is not None and owner.type not in FUNCTIONS:
                owner = owner.parent
            calls.append(dict(id=f'{path}:{node.start_byte}:call', name=name(node),
                              line=node.start_point.row+1, endLine=node.end_point.row+1,
                              callerId=f'{path}:{owner.start_byte}' if owner else None,
                              selection=selection(node, raw), resolution='UNRESOLVED'))
    from flow import analyze
    flow = analyze(functions, path)
    if language == 'python':
        from python_compiler import analyze as compile_python
        flow['compiler'] = compile_python(source, path)
    return dict(path=path, language=language, status='PARSE_ERRORS' if tree.root_node.has_error else 'SYNTAX_ONLY',
                symbols=symbols[:2000], calls=calls[:2000], truncated=len(symbols)>2000 or len(calls)>2000,
                precision='Declarations are syntax facts; calls unresolved. Flow is function-local, flow-insensitive MAY_DEPEND, not compiler/alias analysis.', **flow)
