"""Mask comments/literals for token matching; retain byte and line coordinates."""
import re

LITERALS = re.compile(rb'//[^\r\n]*|/\*[\s\S]*?\*/|(?P<q>"{3,})[\s\S]*?(?P=q)|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'')


def mask(raw):
    return LITERALS.sub(lambda m: bytes(10 if c == 10 else 13 if c == 13 else 32
                                       for c in m[0]), raw)


def padded(replacement, original):
    # Keep newlines at their original offsets, rather than moving multiline expressions.
    if b'\n' in original or b'\r' in original:
        return original
    if len(replacement) > len(original) or b'\n' in replacement or b'\r' in replacement:
        raise ValueError('Recovery must retain source coordinates')
    return replacement + b' ' * (len(original) - len(replacement))
