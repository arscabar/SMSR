import json
from python_compiler import analyze


def verify():
    source = 'def 한글(값):\r\n\t문자="😀\\u2028"; return 값\r\n'
    result = analyze(source, '폴더/한글.py')
    function = result['functions'][1]
    instruction = next(i for i in function['instructions'] if i['opcode'] == 'LOAD_FAST')
    line = source.split('\r\n')[1]
    expected = len(line[:line.rindex('값')].encode('utf-16-le')) // 2
    assert instruction['source']['start'] == dict(line=1, character=expected)
    assert instruction['source']['end']['character'] == expected+1
    # Actual U+2028 inside a string must not change physical line indexing.
    result = analyze('x="\u2028"\ndef f(a): return a\n', 'unicode.py')
    load = next(i for i in result['functions'][1]['instructions'] if i['opcode']=='LOAD_FAST')
    assert load['source']['start']['line'] == 1
    error = analyze('def broken(: # SECRET_SYNTAX_TEXT', 'error.py')
    assert error['status'] == 'COMPILATION_ERRORS' and not error['functions']
    assert 'SECRET_SYNTAX_TEXT' not in json.dumps(error)
    try:
        analyze('\n'.join(f'def f{i}(): return {i}' for i in range(501)), 'limit.py')
        raise AssertionError('Scope cap missing')
    except ValueError:
        pass
    try:
        analyze('def many(x):\n' + ' x += 1\n'*10000, 'limit.py')
        raise AssertionError('Instruction cap missing')
    except ValueError:
        pass
    result = analyze('class A:\n def f(self): return __class__\n', 'class.py')
    owner = next(f for f in result['functions'] if f['name'] == 'A')
    method = next(f for f in result['functions'] if f['name'] == 'A.f')
    assert method['free']['__class__'] == owner['cells']['__class__']
