"""Whitelist Graphify facts into SMSR contracts; never persist source bodies."""
import hashlib
import html
import re
from pathlib import Path
from graphify_snapshot import relative_source
from graphify_entity import details, safe_text
from graphify_support import VERSION

def line_of(value):
    match = re.match(r'^L?(\d+)', str(value or '1'))
    return max(1, min(1000000, int(match[1]))) if match else 1

def normalize(report, root, known):
    if report.get('failed_sources'):
        raise ValueError('Graphify extraction failed; retain previous index')
    nodes, lookup, facts = {}, {}, {}
    owners = {}
    for edge in report['edges']:
        if str(edge.get('relation', '')).upper() in {'CONTAINS', 'HAS_METHOD', 'METHOD', 'DEFINES'}:
            owners.setdefault(str(edge.get('target')), set()).add(str(edge.get('source')))
    for fact in report['nodes']:
        source = relative_source(fact.get('source_file'), root, known)
        if not source:
            continue
        path, label = source['path'], html.unescape(str(fact.get('label', '')))
        if not label or len(label) > 2048:
            raise ValueError('Invalid label')
        if fact.get('file_type') == 'rationale':
            label = safe_text(label)
        file_node = label == Path(path).name and not fact.get('_callable') and fact.get('type') != 'namespace'
        identity = 'file:' + path if file_node else 'symbol:' + hashlib.sha256(
            (path + '\0' + str(fact['id'])).encode()).hexdigest()
        lookup[str(fact['id'])] = identity
        if not file_node:
            facts[identity] = fact
            nodes[identity] = dict(nodeId=identity, ownerPath=path, kind='symbol', label=label,
                sourcePath=path, line=line_of(fact.get('source_location')), hash=source['hash'])
    for identity, node in nodes.items():
        fact = facts[identity]
        parents = owners.get(str(fact['id']), set())
        owner = fact.get('_smsr_owner_id') or (next(iter(parents)) if len(parents) == 1 else None)
        node['details'] = details(fact, lookup, owner)
    from graphify_edges import normalize_edges
    edges, issues = normalize_edges(report, root, known, lookup)
    from graphify_hyperedges import normalize_hyperedges
    return dict(engine='Graphify', version=VERSION, nodes=list(nodes.values()), edges=edges, issues=issues,
                hyperedges=normalize_hyperedges(report, root, known, lookup))
