"""Do not manufacture targets for unresolved Graphify facts."""
import re
from graphify_snapshot import relative_source
from graphify_entity import evidence
from graphify_xaml_links import RELATIONS
from graphify_issue_candidates import names, describe

def normalize_edges(report, root, known, lookup):
    from graphify_normalize import line_of
    edges, issues, seen = [], [], set()
    candidates = names(report, root, known)
    facts = {str(n['id']): n for n in report['nodes']}
    for fact in report['edges']:
        owner = relative_source(fact.get('source_file'), root, known)
        if not owner:
            continue
        relation = str(fact.get('relation', '')).upper()
        if owner['path'].lower().endswith('.xaml') and relation == 'REFERENCES':
            relation = RELATIONS.get(fact.get('context'), relation)
        if not re.fullmatch(r'[A-Z_]{1,64}', relation):
            raise ValueError('Invalid relation')
        source = lookup.get(str(fact.get('source')))
        target = lookup.get(str(fact.get('target')))
        line = line_of(fact.get('source_location'))
        confidence = fact.get('confidence', 'INFERRED')
        if relation == 'CODE_BEHIND' and not str(facts.get(str(fact.get('target')), {}).get('source_file', '')).lower().endswith('.cs'):
            target = None
        if not source or not target or confidence == 'AMBIGUOUS':
            label = str(facts.get(str(fact.get('target')), {}).get('label', '대상 정보 없음')).lstrip('.').removesuffix('()')
            reason, paths = describe(label, candidates.get(label, []), confidence == 'AMBIGUOUS')
            issues.append(dict(ownerPath=owner['path'], sourceLine=line, relation=relation,
                reason=reason, candidatePaths=paths))
            continue
        key = (source, target, relation, owner['path'], line)
        if key in seen:
            continue
        seen.add(key)
        edges.append(dict(sourceId=source, targetId=target, relation=relation, ownerPath=owner['path'],
            sourceLine=line, resolution='RESOLVED' if confidence == 'EXTRACTED' else 'INFERRED',
            confidence='EXTRACTED' if confidence == 'EXTRACTED' else 'INFERRED',
            evidence=evidence(fact, owner)))
    return edges, issues
