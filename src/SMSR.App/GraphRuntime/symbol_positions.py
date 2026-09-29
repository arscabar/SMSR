"""Syntax-selected name spans; no binding or name-based target guessing."""
IDENTIFIERS = {'identifier', 'type_identifier', 'field_identifier',
               'property_identifier', 'name', 'simple_identifier'}

def identifier(node):
    if node is None:
        return None
    if node.type in IDENTIFIERS:
        return node
    for field in ('name', 'function', 'declarator', 'field', 'member', 'method'):
        child = node.child_by_field_name(field)
        if child:
            return identifier(child)
    if node.type in {'function_declaration', 'call_expression'} and node.named_children:
        first = node.named_children[0]
        if first.type in IDENTIFIERS:
            return first
    return None

def name(node):
    token = identifier(node)
    return token.text.decode('utf-8')[:128] if token else ''

def selection(node, raw):
    token = identifier(node)
    if token is None:
        return None
    # ponytail: per-token line prefix; cache line maps if minified files profile hot.
    def position(offset, point):
        prefix = raw[offset-point.column:offset].decode('utf-8')
        return dict(line=point.row,
                    character=len(prefix.encode('utf-16-le')) // 2)
    return dict(start=position(token.start_byte, token.start_point),
                end=position(token.end_byte, token.end_point))
