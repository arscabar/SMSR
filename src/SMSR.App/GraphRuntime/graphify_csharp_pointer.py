"""Equivalent indexing for explicit pointer-cast dereferences the grammar rejects."""
import re
from syntax_source import padded


def balanced(visible, start):
    if start >= len(visible) or visible[start] != 40:
        return None
    depth, cursor = 1, start + 1
    while cursor < len(visible) and depth:
        depth += (visible[cursor] == 40) - (visible[cursor] == 41)
        cursor += 1
    return cursor if not depth else None


def edits(raw, visible):
    result = []
    for match in re.finditer(rb'\*\s*(?=\()', visible):
        start, opening = match.start(), match.end()
        end = balanced(visible, opening)
        if end is None:
            continue
        inner = raw[opening + 1:end - 1]
        if re.match(rb'\s*\(\s*\w+\s*\*\s*\)', visible[opening + 1:end - 1]):
            expression = inner
        elif re.fullmatch(rb'\s*\w+\s*\*\s*', visible[opening + 1:end - 1]):
            cursor = end
            while cursor < len(visible) and visible[cursor] in b' \t':
                cursor += 1
            final = balanced(visible, cursor)
            if final is None:
                continue
            expression, end = raw[opening:final], final
        else:
            continue
        # No literal/comment whitespace is removed; these casts are arithmetic-only.
        if any(c in expression for c in (b'"', b"'", b'/', b'\n', b'\r')):
            continue
        compact = re.sub(rb'[ \t]+', b'', expression)
        replacement = b'(' + compact + b')[0]'
        original = raw[start:end]
        if len(replacement) <= len(original):
            result.append((start, end, padded(replacement, original)))
    return result
