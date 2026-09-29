"""Assignment sites reaching reads; no values, runtime call targets or taint."""
from python_definition_scope import scope
from python_reaching import solve, successors


def analyze(code, function, spend, work):
    reason, boundaries, parameters = scope(code, function)
    result = dict(status='UNAVAILABLE' if reason else 'LOCAL_SLOT_DEFINITIONS',
                  reason=reason, boundaries=boundaries, definitions=[], reads=[], edges=[])
    if reason:
        return result
    normal, exceptional = successors(function, work)
    if len(boundaries) > 2:
        result['status'] = 'PARTIAL_SLOT_DEFINITIONS'
    instructions = function['instructions']
    for name, binding_id in function['locals'].items():
        if name in function['cells']:
            continue
        work(len(instructions))
        initial = dict(id=f'{binding_id}:entry', bindingId=binding_id, name=name, offset=None,
                       source=None, kind='PARAMETER' if name in parameters else 'UNBOUND_ENTRY')
        definitions, writes, reads = [initial], {}, []
        for item in instructions:
            binding = item['binding']
            if not binding or binding['bindingId'] != binding_id or binding['kind'] != 'LOCAL_SLOT':
                continue
            if item['opcode'] in {'STORE_FAST', 'DELETE_FAST'}:
                writes[item['offset']] = 1 << len(definitions)
                definitions.append(dict(id=f"{binding_id}:{item['offset']}", bindingId=binding_id,
                    name=name, offset=item['offset'], source=item['source'],
                    kind='WRITE' if item['opcode'] == 'STORE_FAST' else 'UNBOUND_DELETE'))
            if item['opcode'] in {'LOAD_FAST', 'LOAD_FAST_CHECK'}:
                reads.append(item)
        incoming = solve(instructions[0]['offset'], normal, exceptional, writes, work)
        result['definitions'].extend(definitions)
        spend(len(definitions) + len(reads))
        for item in reads:
            mask = incoming.get(item['offset'], 0)
            candidates = []
            while mask:
                work(1)
                bit = mask & -mask
                candidates.append(definitions[bit.bit_length()-1])
                mask ^= bit
            read_id = f"{binding_id}:{item['offset']}:read"
            result['reads'].append(dict(id=read_id, bindingId=binding_id, name=name,
                offset=item['offset'], source=item['source'], reachable=bool(candidates),
                mayBeUnbound=any(d['kind'].startswith('UNBOUND') for d in candidates)))
            spend(len(candidates))
            result['edges'].extend(dict(source=d['id'], target=read_id, relation='REACHING_DEFINITION') for d in candidates)
    return result
