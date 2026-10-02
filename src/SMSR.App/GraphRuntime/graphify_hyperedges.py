"""Store group membership, never expand it into synthetic pairwise edges."""
import hashlib
import re
from graphify_entity import evidence, safe_text
from graphify_snapshot import relative_source

def normalize_hyperedges(report, root, known, lookup):
    from graphify_normalize import line_of
    items, seen = [], set()
    for fact in report.get('hyperedges', []):
        owner = relative_source(fact.get('source_file'), root, known)
        if not owner:
            continue
        raw = fact.get('nodes', [])
        if not isinstance(raw, list) or not 3 <= len(raw) <= 64:
            raise ValueError('Invalid group membership')
        ids = [lookup.get(str(n)) for n in raw]
        if None in ids or len(set(ids)) != len(ids):
            raise ValueError('Unresolved or repeated group participant')
        relation = str(fact.get('relation', '')).upper()
        if not re.fullmatch(r'[A-Z_]{1,64}', relation):
            raise ValueError('Invalid group relation')
        identity = 'group:' + hashlib.sha256((owner['path'] + '\0' + str(fact['id'])).encode()).hexdigest()
        if identity in seen:
            raise ValueError('Repeated group identity')
        seen.add(identity)
        items.append(dict(hyperedgeId=identity, label=safe_text(fact.get('label')),
            relation=relation, ownerPath=owner['path'], sourceLine=line_of(fact.get('source_location')),
            confidence='EXTRACTED' if fact.get('confidence') == 'EXTRACTED' else 'INFERRED',
            members=[dict(nodeId=n, role='participant') for n in ids], evidence=evidence(fact, owner)))
    return items
