"""Recover only attribute-only preprocessor blocks; never choose an executable branch."""
import re
from graphify_csharp_syntax import normalize
from graphify_csharp_attributes import normalize as attributes

def recover(module, path, value):
    if path.suffix.lower() != '.cs' or not value.get('parse_errors'):
        return value
    raw = path.read_bytes()
    normalized, lines = attributes(raw)
    normalized, syntax = normalize(normalized)
    if not lines and not syntax:
        return value
    result = module._extract_generic(path, module._CSHARP_CONFIG, source_override=normalized)
    if result.get('error') or result.get('parse_errors') or len(result.get('nodes', [])) < len(value.get('nodes', [])):
        return value
    aliases = set(re.findall(rb'\bw\d{3}\b', normalized)) - set(re.findall(rb'\bw\d{3}\b', raw))
    for alias in aliases:
        token = re.compile(r'\b' + alias.decode() + r'\b')
        for node in result.get('nodes', []):
            if isinstance(node.get('label'), str) and node['label'] != path.name:
                node['label'] = token.sub('with', node['label'])
        for call in result.get('raw_calls', []):
            for key in ('receiver', 'callee'):
                if isinstance(call.get(key), str):
                    call[key] = token.sub('with', call[key])
    result['smsr_recovery_lines'] = lines
    result['smsr_syntax_recovery'] = syntax
    return result
