from python_compiler import analyze


def verify():
    result = analyze('''def choose(x):
 if x: return 1
 while x < 3: x += 1
 return x
def protected(x):
 try: return 1 / x
 except ZeroDivisionError: return 0
 finally: x = 2
def stream(items):
 for x in items: yield x
async def wait_for(work):
 return await work
''', 'flow.py')
    functions = {f['name']: f for f in result['functions']}
    choose = functions['choose']
    assert any(e['target'] < e['source'] for e in choose['edges'])
    assert any(sum(e['source'] == i['offset'] for e in choose['edges']) == 2
               for i in choose['instructions'])
    for f in functions.values():
        offsets = {i['offset'] for i in f['instructions']}
        assert all(e['source'] in offsets and e['target'] in offsets for e in f['edges'])
        for i in f['instructions']:
            outgoing = [e for e in f['edges'] if e['source'] == i['offset']]
            if i['opcode'] in {'RETURN_VALUE','RETURN_CONST','RAISE_VARARGS','RERAISE'}:
                assert not outgoing
            if i['opcode'] in {'YIELD_VALUE','RETURN_GENERATOR'}:
                assert outgoing and all(e['kind'] == 'RESUME_AFTER_SUSPEND' for e in outgoing)
        for region in f['exceptionRegions']:
            assert region['target'] in offsets and region['start'] < region['end']
            assert region['kind'] == 'EXCEPTION_REGION'
    assert functions['protected']['exceptionRegions']
    assert not functions['choose']['exceptionRegions']
    assert any(i['opcode'] == 'SEND' for i in functions['wait_for']['instructions'])
