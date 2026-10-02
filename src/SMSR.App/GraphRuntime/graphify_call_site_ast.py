"""Direct C# call locations; nested executable scopes and shadowed names excluded."""
from tree_sitter_language_pack import get_parser
from collections import Counter
from graphify_csharp_attributes import normalize as attributes
from graphify_csharp_syntax import normalize as syntax

SCOPES = {'method_declaration', 'constructor_declaration', 'local_function_statement'}
NESTED = SCOPES | {'lambda_expression', 'anonymous_method_expression'}


def locations(path):
    raw, _ = attributes(path.read_bytes())
    raw, _ = syntax(raw)
    parser = get_parser('csharp')
    tree = parser.parse(raw)
    if tree.root_node.has_error:
        return [], Counter(), {}
    result, declarations, types, pending = [], Counter(), {}, [tree.root_node]
    while pending:
        scope = pending.pop()
        pending.extend(scope.named_children)
        if scope.type not in SCOPES:
            continue
        name = scope.child_by_field_name('name')
        if name:
            declarations[raw[name.start_byte:name.end_byte].decode('utf-8')] += 1
        parent = scope.parent
        while parent and parent.type not in {'class_declaration', 'struct_declaration', 'interface_declaration', 'record_declaration'}:
            parent = parent.parent
        if parent and scope.type != 'local_function_statement':
            types[int(scope.start_point[0]) + 1] = parent.start_byte
        body = scope.child_by_field_name('body')
        if not body:
            continue
        calls, shadow, walk = [], list(), [scope.child_by_field_name('parameters'), body]
        while walk:
            node = walk.pop()
            if node is None or node.type in NESTED:
                continue
            walk.extend(node.named_children)
            if node.type in {'parameter', 'variable_declarator', 'foreach_statement',
                             'single_variable_designation', 'catch_declaration'}:
                name = node.child_by_field_name('name') or node.child_by_field_name('left')
                if name:
                    shadow.append(raw[name.start_byte:name.end_byte].decode('utf-8').lstrip('@'))
            if node.type == 'invocation_expression':
                name = node.child_by_field_name('function')
                if name and name.type == 'member_access_expression':
                    receiver = name.child_by_field_name('expression')
                    if receiver and raw[receiver.start_byte:receiver.end_byte] == b'this':
                        name = name.child_by_field_name('name')
                if name and name.type == 'identifier':
                    calls.append((raw[name.start_byte:name.end_byte].decode('utf-8').lstrip('@'),
                                  int(node.start_point[0]) + 1))
        result.extend((int(scope.start_point[0]) + 1, name, line)
                      for name, line in calls if name not in shadow)
    return result, declarations, types
