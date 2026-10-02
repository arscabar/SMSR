"""Attribute-only preprocessor alternatives; executable alternatives stay untouched."""
import re

BLOCK = re.compile(rb'(?m)^[ \t]*#if[^\r\n]*\r?\n(?P<body>(?:(?!^[ \t]*#(?:if|endif)\b)[\s\S])*?)^[ \t]*#endif[^\r\n]*')
DIRECTIVE = re.compile(rb'(?m)^[ \t]*#(?:if|elif|else|endif)\b[^\r\n]*')


def normalize(raw):
    lines = []
    def replace(match):
        branches = re.split(rb'(?m)^[ \t]*#(?:elif|else)\b[^\r\n]*', match['body'])
        if not all(re.fullmatch(rb'(?:\s*\[[^\[\]#]+\]\s*)+', b) for b in branches):
            return match[0]
        lines.append(raw[:match.start()].count(b'\n') + 1)
        return DIRECTIVE.sub(lambda d: b' ' * len(d[0]), match[0])
    return BLOCK.sub(replace, raw), lines
