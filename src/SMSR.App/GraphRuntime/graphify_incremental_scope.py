"""Conservative name-key and reverse-edge impact closure, including unresolved calls."""
import html
import re
from pathlib import Path
from graphify_incremental_context import identifiers, manifest
from graphify_incremental_safety import risk

def names(nodes, paths):
    result = set()
    for node in nodes:
        if node.get('source_file') not in paths or node.get('type') == 'namespace':
            continue
        if node.get('label') == Path(node['source_file']).name:
            continue
        result.update(re.findall(r'[^\W\d]\w*', html.unescape(str(node.get('label', '')))))
    for path in paths:
        result.add(Path(path).stem)
    return result

def select(module, root, known, paths, previous, request):
    current = manifest(known)
    all_paths = {p.relative_to(root).as_posix() for p in paths}
    if request.get('forceFullReason') or previous is None:
        return all_paths, request.get('forceFullReason') or '해시·분석 버전이 일치하는 컨텍스트 없음'
    old = previous['manifest']
    changed = {p for p in current.keys() | old.keys() if current.get(p) != old.get(p)}
    keys = identifiers(root, known)
    problem = risk(root, previous, current, changed, all_paths)
    if problem:
        return all_paths, problem
    fresh_nodes = []
    for path in sorted(changed & all_paths):
        target = root / path
        value = module.load_cached(target, root, cache_root=root)
        if value is None:
            value = module._safe_extract_with_xaml_root(module._get_extractor(target), target, root)
            if value.get('error') or not value.get('nodes') or value.get('parse_errors'):
                return all_paths, '변경 파일 구문/추출 진단으로 영향 범위 확정 불가'
            module.save_cached(target, value, root, cache_root=root)
        for node in value['nodes']:
            fresh_nodes.append(dict(node, source_file=path))
    lookup = names(previous['report']['nodes'], changed) | names(fresh_nodes, changed)
    affected = changed & all_paths
    affected |= {p for p in all_paths if lookup.intersection(keys[p])}
    owners = {str(n['id']): n['source_file'] for n in previous['report']['nodes']}
    semantic = {'calls', 'uses', 'references', 'imports', 'imports_from', 'inherits', 'implements'}
    while True:
        before = set(affected)
        for edge in previous['report']['edges']:
            if edge.get('relation') in semantic and owners.get(str(edge.get('target'))) in affected | changed:
                owner = edge.get('source_file')
                if owner in all_paths:
                    affected.add(owner)
        if before == affected:
            break
    return affected, None
