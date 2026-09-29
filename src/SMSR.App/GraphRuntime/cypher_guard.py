"""Read-query boundary. Never expose Kuzu procedures, extensions or file functions."""
import re

FUNCTIONS = set('count sum avg min max collect coalesce nullif abs ceil floor round sqrt pow power sign log log10 exp sin cos tan size length lower upper trim ltrim rtrim replace reverse substring left right split concat starts_with ends_with contains tolower toupper tostring tointeger tofloat toboolean labels label type id nodes relationships range head last tail properties keys list_extract list_slice list_contains list_concat array_length date timestamp duration extract all any none single exists shortestpath allshortestpaths cast'.split())
STRUCTURAL = {'match', 'where', 'in', 'not', 'and', 'or', 'xor', 'when', 'return', 'with', 'by', 'as', 'on', 'optional'}
FORBIDDEN = set('call load copy install attach detach create merge delete set remove drop alter export import macro begin commit rollback use database extension checkpoint comment explain profile'.split())

def validate(query):
    if not isinstance(query, str) or not 1 <= len(query) <= 8000:
        raise ValueError('Cypher must contain 1–8000 characters')
    # Replace literals/comments before inspecting executable tokens; malformed quotes fail closed.
    pattern = r"'(?:\\.|''|[^'\\])*'|\"(?:\\.|\"\"|[^\"\\])*\"|`(?:``|[^`])*`|//[^\n]*|/\*[\s\S]*?\*/"
    clean = re.sub(pattern, lambda m: ' quoted_identifier ' if m[0].startswith('`') else ' ', query)
    if any(c in clean for c in "'\"`;\\"):
        raise ValueError('Multiple statements or malformed literals are not allowed')
    tokens = re.findall(r'[a-zA-Z_][a-zA-Z_0-9]*', clean.lower())
    if not tokens or tokens[0] not in {'match', 'optional', 'return', 'with', 'unwind'} or FORBIDDEN.intersection(tokens):
        raise ValueError('Only read-only Cypher clauses are allowed')
    for match in re.finditer(r'([a-zA-Z_][a-zA-Z_0-9]*)\s*\(', clean.lower()):
        if match[1] not in FUNCTIONS | STRUCTURAL:
            raise ValueError('Function is outside the safe scalar/aggregate allowlist')
    return query
