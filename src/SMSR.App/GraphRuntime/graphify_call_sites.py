"""Preserve repeated direct call sites only for an existing unique static binding."""
from collections import defaultdict
from graphify_call_site_ast import locations
from graphify_normalize import line_of


def augment(report, paths, results, module, root):
    nodes = {str(n['id']): n for n in report['nodes']}
    owners, names, bindings = defaultdict(list), defaultdict(list), defaultdict(list)
    for key, node in nodes.items():
        if node.get('_callable') and not node.get('_callable_class'):
            owners[(str(node.get('source_file')), line_of(node.get('source_location')))].append(key)
            names[str(node.get('label', '')).lstrip('.').removesuffix('()')].append(key)
    seen = set()
    for edge in report['edges']:
        if edge.get('relation') != 'calls':
            continue
        target = nodes.get(str(edge.get('target')), {})
        name = str(target.get('label', '')).lstrip('.').removesuffix('()')
        seen.add((str(edge['source']), str(edge['target']), str(edge.get('source_file')),
                  line_of(edge.get('source_location'))))
        if edge.get('confidence') in {'EXTRACTED', 'INFERRED'}:
            bindings[(str(edge['source']), name)].append(edge)
    for path in paths:
        if path.suffix.lower() != '.cs':
            continue
        owner = path.relative_to(root).as_posix()
        sites, declarations, types = locations(path)
        for declaration, name, line in sites:
            callers = owners.get((owner, declaration), [])
            if len(callers) != 1 or len(names.get(name, [])) != 1 or declarations[name] != 1:
                continue
            edges = bindings.get((callers[0], name), [])
            if not edges or len({str(e['target']) for e in edges}) != 1:
                continue
            original = edges[0]
            if str(nodes[str(original['target'])].get('source_file')) != owner:
                continue
            target_line = line_of(nodes[str(original['target'])].get('source_location'))
            if declaration not in types or types.get(target_line) != types[declaration]:
                continue
            key = (callers[0], str(original['target']), owner, line)
            if key not in seen:
                seen.add(key)
                report['edges'].append(dict(original, source_file=owner, source_location=f'L{line}'))
