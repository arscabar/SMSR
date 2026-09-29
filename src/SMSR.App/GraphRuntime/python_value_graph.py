"""Instruction-level operand candidates, never evaluated constants or taint."""


def build(function, spend, work):
    nodes, edges, keys = {}, [], set()
    locations = {i['offset']: i['source'] for i in function['instructions']}

    def add(node):
        if node['id'] not in nodes:
            spend(1)
            nodes[node['id']] = node
        return frozenset([node['id']])

    def link(origins, target, relation, operand=None):
        for origin in sorted(origins):
            work(1)
            key = (origin, target, relation, operand)
            if key not in keys:
                spend(1)
                keys.add(key)
                edges.append(dict(source=origin, target=target, relation=relation, operand=operand))

    def emit(item, kind, inputs=(), suffix='', **extra):
        identity = f"{function['id']}:value:{item.offset}:{kind}{suffix}"
        result = add(dict(id=identity, kind=kind, offset=item.offset,
                          source=locations[item.offset], opcode=item.opname, **extra))
        for index, origins in enumerate(inputs):
            link(origins, identity, 'OPERAND_CANDIDATE', index)
        return result

    model = function['definitions']
    for node in model['definitions']:
        add(dict(node))
    for node in model['reads']:
        add(dict(node, kind='READ'))
    for edge in model['edges']:
        link([edge['source']], edge['target'], edge['relation'])
    return nodes, edges, emit, link
