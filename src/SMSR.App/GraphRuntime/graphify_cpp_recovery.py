"""Windows declaration annotations only; do not expand arbitrary C++ macros."""
import re
from syntax_source import mask, padded

CONVENTION = rb'\b(?:WINAPI|CALLBACK|APIENTRY|STDMETHODCALLTYPE|STDAPICALLTYPE|__stdcall|__cdecl|__fastcall|__vectorcall|__thiscall)\b(?=\s+(?:\w|\*))'
SAL = rb'\b(?:__in|__out|__inout|__in_opt|__out_opt|__deref_out|__deref_out_opt|_In_|_Out_|_Inout_|_In_opt_|_Out_opt_|_COM_Outptr_)\b(?=\s+\w)'


def recover(module, path, value):
    if path.suffix.lower() not in {'.c', '.cpp', '.cc', '.cxx', '.h', '.hpp'} or not value.get('parse_errors'):
        return value
    raw = path.read_bytes()
    visible, edits = mask(raw), []
    for pattern, replacement in [(CONVENTION, b''), (SAL, b''),
                                 (rb'\b__override\b(?=\s+~?\w)', b''),
                                 (rb'\b(?:IFACEMETHODIMP|STDMETHODIMP)\b', b'HRESULT'),
                                 (rb'\bEXTERN_C\b', b'extern')]:
        for match in re.finditer(pattern, visible):
            edits.append((match.start(), match.end(), padded(replacement, match[0])))
    for match in re.finditer(rb'\bSTDMETHOD_\s*\(\s*(\w+)\s*,\s*(\w+)\s*\)', visible):
        edits.append((match.start(), match.end(), padded(match[1] + b' ' + match[2], match[0])))
    for match in re.finditer(rb'\bSTDMETHOD\s*\(\s*(\w+)\s*\)', visible):
        edits.append((match.start(), match.end(), padded(b'HRESULT ' + match[1], match[0])))
    normalized = raw
    for start, end, replacement in sorted(edits, reverse=True):
        normalized = normalized[:start] + replacement + normalized[end:]
    # .h defaults to C upstream; C++ default parameters/references need the C++ grammar.
    cpp_header = path.suffix.lower() == '.h' and bool(re.search(rb'\b(?:class|namespace|bool)\b|\w\s*&\s*\w|\w\s*=\s*(?:true|false)', visible))
    if not edits and not cpp_header:
        return value
    config = module._C_CONFIG if path.suffix.lower() == '.c' else module._CPP_CONFIG
    result = module._extract_generic(path, config, source_override=normalized)
    if result.get('error') or result.get('parse_errors') or len(result.get('nodes', [])) < len(value.get('nodes', [])):
        return value
    result['smsr_syntax_recovery'] = [(raw[:start].count(b'\n') + 1, 'windows-declaration')
                                      for start, _, _ in edits]
    if cpp_header:
        result['smsr_syntax_recovery'].append((1, 'cpp-header-grammar'))
    return result
