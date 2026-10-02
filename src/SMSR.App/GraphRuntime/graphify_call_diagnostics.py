"""Keep unmatched raw call sites visible without guessing their target."""
import re
from graphify_normalize import line_of
from graphify_snapshot import relative_source
from graphify_issue_candidates import names, describe

def unmatched(report, paths, results, module, root, known):
    nodes = {str(n['id']): n for n in report['nodes']}
    candidates = names(report, root, known)
    covered = set()
    for edge in report['edges']:
        if edge.get('relation') != 'calls' or edge.get('confidence') == 'AMBIGUOUS':
            continue
        owner = relative_source(edge.get('source_file'), root, known)
        target = nodes.get(str(edge.get('target')), {})
        if not owner or not relative_source(target.get('source_file'), root, known):
            continue
        name = str(target.get('label', '')).lstrip('.').removesuffix('()')
        covered.add((owner['path'], line_of(edge.get('source_location')), name))
    seen = set()
    for path in paths:
        value = results.get(path) or module.load_cached(path, root, cache_root=root) or {}
        owner = path.relative_to(root).as_posix()
        for call in value.get('raw_calls', []):
            name = str(call.get('callee', ''))
            if not re.fullmatch(r'[\w.$:<>]{1,256}', name) or not call.get('source_location'):
                continue
            line = line_of(call['source_location'])
            if (owner, line, name) in covered:
                continue
            receiver = str(call.get('receiver') or '')
            receiver = receiver if re.fullmatch(r'[\w.]{1,128}', receiver) else ''
            label = (receiver + '.' if receiver else '') + name
            key = (owner, line, label)
            if key in seen:
                continue
            seen.add(key)
            reason, candidate_paths = describe(label, candidates.get(name, []))
            yield dict(ownerPath=owner, sourceLine=line, relation='CALLS',
                reason=reason, candidatePaths=candidate_paths)
