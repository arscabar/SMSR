"""Annotate existing C# declaration facts only at unique AST name/line matches."""
from collections import defaultdict
from tree_sitter_language_pack import get_parser
from graphify_csharp_attributes import normalize as attributes
from graphify_csharp_syntax import normalize as syntax

KINDS = {'class_declaration': 'class', 'record_declaration': 'class',
         'interface_declaration': 'interface', 'struct_declaration': 'struct',
         'enum_declaration': 'enum', 'enum_member_declaration': 'enum_member',
         'property_declaration': 'property', 'method_declaration': 'method',
         'constructor_declaration': 'method', 'local_function_statement': 'function'}

def annotate(path, value):
    if path.suffix.lower() != '.cs' or value.get('parse_errors'):
        return value
    raw, _ = attributes(path.read_bytes())
    raw, _ = syntax(raw)
    tree = get_parser('csharp').parse(raw)
    if tree.root_node.has_error:
        return value
    facts, pending = defaultdict(list), [tree.root_node]
    while pending:
        node = pending.pop()
        pending.extend(node.named_children)
        kind = KINDS.get(node.type)
        name = node.child_by_field_name('name')
        if kind and name:
            label = raw[name.start_byte:name.end_byte].decode('utf-8').lstrip('@')
            facts[(int(node.start_point[0]) + 1, label)].append(
                (kind, int(node.end_point[0]) + 1))
    for fact in value.get('nodes', []):
        location = str(fact.get('source_location', ''))
        if not location.startswith('L') or not location[1:].isdigit():
            continue
        label = str(fact.get('label', '')).lstrip('.').removesuffix('()').lstrip('@')
        matches = facts.get((int(location[1:]), label), [])
        if len(matches) == 1:
            fact['_smsr_kind'], fact['_smsr_end_line'] = matches[0]
    classes = list({n['id']: n for n in value.get('nodes', []) if n.get('_smsr_kind') == 'class'}.values())
    for fact in value.get('nodes', []):
        if fact.get('_smsr_kind') not in {'property', 'method', 'field'}:
            continue
        line = int(str(fact['source_location']).lstrip('L'))
        parents = [n for n in classes if int(str(n['source_location']).lstrip('L')) <= line <= n['_smsr_end_line']]
        parents.sort(key=lambda n: n['_smsr_end_line'] - int(str(n['source_location']).lstrip('L')))
        if parents and (len(parents) == 1 or parents[0]['_smsr_end_line'] != parents[1]['_smsr_end_line']):
            fact['_smsr_owner_id'] = parents[0]['id']
    return value

def annotate_report(report, paths, root):
    grouped = defaultdict(list)
    for node in report['nodes']:
        grouped[str(node.get('source_file'))].append(node)
    for path in paths:
        nodes = grouped.get(path.relative_to(root).as_posix(), [])
        if nodes:
            annotate(path, dict(nodes=nodes))
