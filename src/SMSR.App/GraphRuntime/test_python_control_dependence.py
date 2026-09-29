from python_compiler import analyze
from test_control_postdominance import verify as oracle

SOURCE = '''def choose(flag, value):
 if flag:
  value = 1
 else:
  value = 2
 return value
def joined(flag, value):
 if flag: value = 1
 else: value = 2
 value += 1
 value *= 2
 value -= 3
 return unknown(value)
def early(flag):
 if flag: return 1
 return 0
def repeat(flag):
 while flag:
  flag = unknown()
 return flag
def walk(items):
 for item in items:
  if item: break
 else: return 0
 return 1
def forever(flag):
 if flag:
  while True: pass
 return 0
def protected(x):
 try: return 1 / x
 finally: x = 0
def stream(x):
 yield x
def optional(x):
 if x is None: return 0
 return 1
raise RuntimeError("TARGET_MUST_NOT_RUN")
'''


def verify():
    result = analyze(SOURCE, 'control.py')
    assert result == analyze(SOURCE, 'control.py')
    fs = {f['name']:f for f in result['functions']}
    for name, reason in [('forever','NON_EXIT_REACHABLE_REGION'),
                         ('protected','EXCEPTION_REGIONS'), ('stream','SUSPENSION')]:
        c = fs[name]['control']
        assert c['status']=='UNAVAILABLE' and c['reason']==reason, (name,c)
        assert not c['links'] and not c['postdominators'] and not c['transfers']
    choose = fs['choose']
    c = choose['control']
    instructions = {i['offset']:i for i in choose['instructions']}
    stores = [i for i in instructions.values() if i['opcode']=='STORE_FAST']
    for item, expected in zip(stores, ['TRUE','FALSE']):
        assert [e['outcome'] for e in c['links'] if e['dependent']==item['offset']]==[expected]
    # CPython duplicates a short return tail: both instruction copies are conditional.
    returns = [i['offset'] for i in instructions.values() if i['opcode']=='RETURN_VALUE']
    assert len(returns)==2
    assert {e['outcome'] for e in c['links'] if e['dependent'] in returns}=={'TRUE','FALSE'}
    joined = fs['joined']
    returns = [i['offset'] for i in joined['instructions'] if i['opcode']=='RETURN_VALUE']
    assert len(returns)==1
    assert not any(e['dependent'] in returns for e in joined['control']['links'])
    loop = fs['repeat']['control']
    assert loop['status']=='NORMAL_CFG_CONTROL_DEPENDENCE'
    assert any(e['controller']==e['dependent'] for e in loop['links'])
    walk = fs['walk']['control']
    assert any(e['kind']=='ITERATION_EXHAUSTED' and 'skippedOffset' in e for e in walk['transfers'])
    assert {'NONE','NOT_NONE'} <= {e['outcome'] for e in fs['optional']['control']['links']}
    for f in fs.values():
        offsets = {i['offset'] for i in f['instructions']}
        assert all(e['controller'] in offsets and e['dependent'] in offsets for e in f['control']['links'])
    oracle()
    print('Python control dependence self-check passed')


if __name__ == '__main__':
    verify()
