"""Run with the configured CPython 3.12 graph runtime; no target code executes."""
import json
from python_compiler import analyze


def main():
    source = '''import smsr_module_that_does_not_exist
SECRET = "PYTHON_LITERAL_NOT_STORED"
def outer(value):
    local = value
    def inner():
        nonlocal local
        local += 1
        return local
    return inner
def shadow(value):
    value = 2
    return value + missing_global
class Box:
    local = 3
    def method(self):
        return local
one = lambda x: x; two = lambda x: x
raise RuntimeError("DO_NOT_EXECUTE")
'''
    result = analyze(source, 'scope.py')
    assert result['status'] == 'COMPILER_BYTECODE'
    assert 'PYTHON_LITERAL_NOT_STORED' not in json.dumps(result)
    assert 'DO_NOT_EXECUTE' not in json.dumps(result)
    functions = result['functions']
    outer = next(f for f in functions if f['name'] == 'outer')
    inner = next(f for f in functions if f['name'].endswith('.inner'))
    assert inner['free']['local'] == outer['cells']['local']
    refs = [i['binding'] for i in inner['instructions'] if i['opcode'] in {'LOAD_DEREF','STORE_DEREF'}]
    assert refs and all(r['bindingId'] == outer['cells']['local'] for r in refs)
    shadow = next(f for f in functions if f['name'] == 'shadow')
    assert shadow['locals']['value'] != outer['locals']['value']
    method = next(f for f in functions if f['name'] == 'Box.method')
    ref = next(i['binding'] for i in method['instructions'] if i['opcode'] == 'LOAD_GLOBAL')
    assert ref['name'] == 'local' and ref['bindingId'] is None
    lambdas = [f for f in functions if f['name'] == '<lambda>']
    assert len(lambdas) == 2 and lambdas[0]['id'] != lambdas[1]['id']
    assert all(i['binding']['bindingId'] for f in functions for i in f['instructions']
               if i['binding'] and i['binding']['kind'] in {'LOCAL_SLOT','CELL_SLOT'})
    from test_python_control import verify
    verify()
    from test_python_boundaries import verify as boundaries
    boundaries()
    from test_python_definitions import main as definitions
    definitions()
    from test_python_values import verify as values
    values()
    from test_python_protocols import verify as protocols
    protocols()
    from test_python_control_dependence import verify as dependence
    dependence()
    print('Python compiler self-check passed')


if __name__ == '__main__':
    main()
