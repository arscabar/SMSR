"""Preserve only supported entity metadata; absent types remain unknown."""
import re
from graphify_support import VERSION

KINDS = {'class', 'method', 'function', 'interface', 'struct', 'enum', 'property',
         'field', 'enum_member', 'namespace', 'concept', 'rationale'}
TYPES = {'code', 'document', 'paper', 'image', 'rationale', 'concept'}
SECRET = re.compile(r'(?i)(?:sk-[\w-]{16,}|smds_[\w-]{16,}|bearer\s+\S+|'
                    r'(?:api[_-]?key|password|secret|token)\s*[:=]\s*[\"\x27]?[^\s\"\x27]{8,})')

def safe_text(value):
    value = str(value or '')[:512]
    return '[비밀값 제외]' if SECRET.search(value) else value

def details(fact, lookup, owner=None):
    kind = str(fact.get('_smsr_kind') or fact.get('type', '')).lower()
    file_type = fact.get('file_type', 'code')
    if kind not in KINDS:
        kind = 'class' if fact.get('_callable_class') else (
            'method' if str(fact.get('label', '')).startswith('.') else 'function'
        ) if fact.get('_callable') else 'unknown'
    if file_type in {'rationale', 'concept'}:
        kind = file_type
    value = dict(entityKind=kind, fileType=file_type if file_type in TYPES else 'code',
                 analyzer='Graphify', analyzerVersion=VERSION)
    end = re.fullmatch(r'L?(\d+)-L?(\d+)', str(fact.get('source_location', '')))
    if end and int(end[2]) >= int(end[1]) and int(end[2]) <= 1000000:
        value['endLine'] = int(end[2])
    location = str(fact.get('source_location', ''))
    if re.fullmatch(r'L?\d+(?:-L?\d+)?', location) and len(location) <= 256:
        value['sourceLocation'] = location
    if isinstance(fact.get('_smsr_end_line'), int):
        value['endLine'] = fact['_smsr_end_line']
    if owner in lookup:
        value['ownerNodeId'] = lookup[owner]
    why = safe_text(fact.get('rationale'))
    if why:
        value['rationale'] = why
    return value

def evidence(fact, owner):
    value = dict(analyzer='Graphify', analyzerVersion=VERSION, sourceHash=owner['hash'])
    score = fact.get('confidence_score')
    if isinstance(score, (int, float)) and not isinstance(score, bool) and 0 <= score <= 1:
        value['confidenceScore'] = score
    return value
