"""Link simple bindings only to unique, AST-owned ViewModel properties."""
import re
from pathlib import Path
from collections import defaultdict

RELATIONS = {'x_class': 'CODE_BEHIND', 'view_model': 'VIEW_MODEL',
             'event': 'HANDLES_EVENT', 'binding_path': 'BINDS_TO',
             'binding_command': 'COMMAND_BINDS_TO', 'binding_converter': 'CONVERTER'}

def augment(report, root):
    report['edges'] = [e for e in report['edges'] if not e.get('_smsr_binding')]
    nodes = {n['id']: n for n in report['nodes']}
    members, views, groups = defaultdict(list), defaultdict(set), defaultdict(list)
    for node in report['nodes']:
        if node.get('_smsr_kind') == 'property' and node.get('_smsr_owner_id'):
            members[(node['_smsr_owner_id'], node['label'])].append(node)
    for edge in report['edges']:
        source = str(edge.get('source_file', ''))
        if Path(source).suffix.lower() != '.xaml':
            continue
        groups[source].append(edge)
        if edge.get('context') == 'view_model':
            views[source].add((edge['target'], edge.get('confidence', 'INFERRED')))
    extra = []
    for source, edges in groups.items():
        path = (root / source).resolve()
        if not path.is_relative_to(root.resolve()) or len(views[source]) != 1 or not path.is_file():
            continue
        raw = path.read_bytes()
        # ponytail: conservative whole-file gate; scoped DataContext resolution can widen coverage later.
        if len(raw) > 2_000_000 or re.search(br'ElementName|RelativeSource|\bSource\s*=|<(?:\w+:)?(?:DataTemplate|ControlTemplate|HierarchicalDataTemplate)\b|<!\s*(DOCTYPE|ENTITY)\b', raw, re.I):
            continue
        if len(re.findall(br'\bDataContext\b', raw)) > 2:
            continue
        vm, confidence = next(iter(views[source]))
        for edge in edges:
            if edge.get('context') not in {'binding_path', 'binding_command'}:
                continue
            name = nodes.get(edge['target'], {}).get('label', '')
            if not re.fullmatch(r'[A-Za-z_]\w*', name):
                continue
            matches = members.get((vm, name), [])
            if len(matches) == 1:
                extra.append(dict(edge, target=matches[0]['id'], confidence=confidence, _smsr_binding=True))
    seen = {(e['source'], e['target'], e.get('context'), e.get('source_location')) for e in report['edges']}
    for edge in extra:
        key = (edge['source'], edge['target'], edge.get('context'), edge.get('source_location'))
        if key not in seen:
            report['edges'].append(edge)
            seen.add(key)
