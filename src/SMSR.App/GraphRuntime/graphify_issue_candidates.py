"""Candidates are diagnostics, never proof of a callable target."""
from collections import defaultdict
from graphify_snapshot import relative_source
from graphify_entity import safe_text

def names(report, root, known):
    result = defaultdict(set)
    for node in report['nodes']:
        if not node.get('_callable') and node.get('_smsr_kind') not in {'class', 'interface', 'struct', 'method', 'function'}:
            continue
        owner = relative_source(node.get('source_file'), root, known)
        if owner:
            name = str(node.get('label', '')).lstrip('.').removesuffix('()')
            result[name].add(owner['path'])
    return result

def describe(label, candidates, ambiguous=False):
    paths = sorted(candidates)[:12]
    category = 'AMBIGUOUS_TARGET' if ambiguous else 'UNVERIFIED_LOCAL_TARGET' if paths else 'EXTERNAL_OR_MISSING_SOURCE'
    return (f'{category} · {safe_text(label)} · 후보는 확인용이며 호출 대상 확정이 아님', paths)
