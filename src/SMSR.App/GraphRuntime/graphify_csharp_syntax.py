"""Equivalent C# syntax for grammar gaps; no executable branch selection."""
import re
from syntax_source import mask, padded
from graphify_csharp_pointer import edits as pointer_casts


def normalize(raw):
    visible = mask(raw)
    pointers = set(re.findall(rb'\b\w+\s*\*\s*(\w+)', visible))
    pointers.update(re.findall(rb'\bvar\s+(\w+)\s*=\s*\(\s*\w+\s*\*\s*\)', visible))
    edits = pointer_casts(raw, visible)
    notes = [(raw[:start].count(b'\n') + 1, 'pointer-cast-index') for start, _, _ in edits]
    for match in re.finditer(rb'\*\s*\(', visible):
        start, cursor, depth = match.start(), match.end(), 1
        while cursor < len(visible) and depth:
            depth += (visible[cursor] == 40) - (visible[cursor] == 41)
            cursor += 1
        if depth:
            continue
        inner = visible[match.end():cursor - 1]
        terms = re.fullmatch(rb'\s*(\w+)\s*\+\s*(.+)', inner)
        if not terms or terms[1] not in pointers:
            continue
        original = raw[start:cursor]
        offset_start = match.end() + terms.start(2)
        replacement = terms[1] + b'[' + raw[offset_start:cursor - 1].rstrip() + b']'
        changed = padded(replacement, original)
        if changed != original:
            edits.append((start, cursor, changed))
            notes.append((raw[:start].count(b'\n') + 1, 'pointer-index'))
    # `with` is contextual, but the grammar treats a foreach local as a keyword.
    if re.search(rb'\b(?:var|foreach\s*\(\s*var)\s+with\b', visible):
        alias = next((b'w' + str(i).encode().rjust(3, b'0') for i in range(1000)
                      if not re.search(rb'\bw' + str(i).encode().rjust(3, b'0') + rb'\b', raw)), None)
        if alias:
            for match in re.finditer(rb'\bwith\b(?!\s*\{)', visible):
                edits.append((match.start(), match.end(), alias))
                notes.append((raw[:match.start()].count(b'\n') + 1, 'contextual-identifier'))
    result = raw
    for start, end, changed in sorted(edits, reverse=True):
        result = result[:start] + changed + result[end:]
    return result, notes
