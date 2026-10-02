"""Partial resolver with preserved endpoints and full-scope safety fallback."""
from graphify_incremental import merge
from graphify_incremental_scope import select
from graphify_normalize import normalize

def drift(previous, result, affected):
    for key in ('nodes', 'edges', 'issues', 'hyperedges'):
        def kept(value):
            return {str(sorted(item.items())) for item in value[key] if item['ownerPath'] not in affected}
        if kept(previous) != kept(result):
            return True
    return False

def extract_scope(module, root, known, paths, previous, request):
    affected, reason = select(module, root, known, paths, previous, request)
    selected = [p for p in paths if p.relative_to(root).as_posix() in affected]
    context = previous['report'] if previous and len(selected) < len(paths) else None
    fresh = module.extract(selected, root=root, cache_root=root, parallel=False,
        resolution_context_nodes=[n for n in context['nodes'] if n['source_file'] not in affected] if context else None,
        resolution_context_edges=[e for e in context['edges'] if e.get('source_file') not in affected] if context else None)
    try:
        report = merge(module, previous if context else None, fresh, affected, known)
        result = normalize(report, root, known)
        uncertain = context and (result['issues'] or drift(normalize(previous['report'], root, known), result, affected))
    except ValueError as cause:
        if not context or str(cause) != 'Partial symbol identity collision':
            raise
        uncertain = True
    if uncertain:
        # ponytail: global-merge/ambiguous drift is unsafe; keep the exact full resolver.
        selected, context = paths, None
        reason = '부분 해석의 미해소·ID 병합·보존 관계 정합성이 불확실해 전체 분석'
        report = module.extract(paths, root=root, cache_root=root, parallel=False)
        result = normalize(report, root, known)
    return report, result, selected, context, reason
