import dis
import json
from python_compiler import analyze
from python_values import analyze as values


def verify():
    from test_python_values import model
    cases = {
        'def f(x):\n return target(*x)': 'UNSUPPORTED_OPCODE:CALL_FUNCTION_EX',
        'def f(x):\n yield x': 'SUSPENSION_STACK',
        'def f(x):\n try: return x\n except: return 0': 'EXCEPTION_STACK_UNWINDING',
        'def f(x):\n def inner(): return x\n return inner': 'SHARED_CELLS',
        'def f(x):\n return [v for v in x]': 'STACK_SAVED_SLOT_RESTORATION',
    }
    for source, reason in cases.items():
        data = model(source)
        assert data['reason']==reason, (source,data)
        assert data['nodes']==data['edges']==[]
    source = 'def f(x):\n return unknown("VALUE_SECRET_NOT_SAVED", unusual_keyword=x)'
    data = model(source)
    assert data['status']=='VALUE_FLOW_CANDIDATES'
    serialized = json.dumps(data)
    assert 'VALUE_SECRET_NOT_SAVED' not in serialized and 'unusual_keyword' not in serialized
    ids = {n['id'] for n in data['nodes']}
    assert all(e['source'] in ids and e['target'] in ids for e in data['edges'])
    assert '<call-null>' not in serialized
    code = compile('def f(x):\n return x+1','<test>','exec').co_consts[0]
    function = analyze('def f(x):\n return x+1','budget.py')['functions'][1]
    def reject(_):
        raise ValueError('test budget')
    for spend,work in ((reject,lambda _:None),(lambda _:None,reject)):
        try:
            values(code,function,list(dis.get_instructions(code)),spend,work)
        except ValueError as error:
            assert str(error)=='test budget'
        else:
            raise AssertionError('Value budget ignored')
