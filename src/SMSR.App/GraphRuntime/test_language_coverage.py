"""All 16 grammars must expose declarations, calls and lexical caller ownership."""
from language_cases import CASES
from symbols import LANGUAGES, run

assert set(CASES) == set(LANGUAGES.values()), 'Missing grammar oracle'
for language, (extension, source) in CASES.items():
    result = run(dict(path='sample.' + extension, source=source))
    assert result['status'] == 'SYNTAX_ONLY', (language, result['status'])
    declarations = {s['name']: s for s in result['symbols']}
    assert {'target', 'entry'} <= declarations.keys(), (language, declarations)
    calls = [call for call in result['calls'] if call['name'] == 'target']
    assert len(calls) == 1, (language, result['calls'])
    assert calls[0]['callerId'] == declarations['entry']['id'], (language, calls)
    assert calls[0]['resolution'] == 'UNRESOLVED', 'Syntax must not imply type resolution'
    for item in [*result['symbols'], *result['calls']]:
        span = item['selection']
        if not item['name']:
            assert span is None
            continue
        line = source.split('\n')[span['start']['line']].encode('utf-16-le')
        selected = line[span['start']['character']*2:span['end']['character']*2].decode('utf-16-le')
        assert selected == item['name'], (language, item, selected)
    assert 'return target(1)' not in str(result), 'Source text leaked'
print('16 language declaration/call/caller oracles passed; semantic resolution remains separate')

source = 'def entry():\r\n    note = "한글😀"; target(1); target(2)\r\n'
calls = run(dict(path='unicode.py', source=source))['calls']
line = source.split('\n')[1].encode('utf-16-le')
assert len(calls) == 2 and calls[0]['selection'] != calls[1]['selection']
for call in calls:
    span = call['selection']
    assert span['start']['line'] == 1
    assert line[span['start']['character']*2:span['end']['character']*2].decode('utf-16-le') == 'target'
print('UTF-16 Korean/emoji/CRLF and same-line call positions passed')
