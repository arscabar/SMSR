"""Trace only this trusted fixture, never repository targets, as a CFG oracle."""
import inspect
import sys
from python_compiler import analyze
from test_python_values import model


def fixture(items, stop):
    total = 0
    for value in items:
        if value < 0:
            continue
        if value == stop:
            break
        total += value
    else:
        total += 1
    return total


def verify():
    source = inspect.getsource(fixture)
    function = next(f for f in analyze(source,'fixture.py')['functions'] if f['name']=='fixture')
    data = function['values']
    assert data['status']=='VALUE_FLOW_CANDIDATES', data['reason']
    graph = {(e['source'],e['target']) for e in data['transfers']}
    seen = set()
    for items,stop in (([],9),([1,2],9),([-1,1,2],9),([1,2,3],2)):
        offsets = []
        def trace(frame,event,arg):
            if frame.f_code is fixture.__code__:
                frame.f_trace_opcodes = True
                if event=='opcode':
                    offsets.append(frame.f_lasti)
                return trace
        previous = sys.gettrace()
        try:
            sys.settrace(trace)
            fixture(items,stop)
        finally:
            sys.settrace(previous)
        observed = set(zip(offsets,offsets[1:]))
        assert observed <= graph, observed-graph
        seen.update(observed)
    for edge in data['transfers']:
        if edge['kind'].startswith('ITERATION_'):
            assert (edge['source'],edge['target']) in seen
        if edge['kind']=='ITERATION_EXHAUSTED':
            assert edge['target'] != edge['skippedOffset']
    for source in ('def f(xs,ys):\n for x in xs:\n  for y in ys: target(x,y)',
                   'def f(xs):\n for x in xs: return x\n return 0',
                   'def f(xs):\n for x in xs: pass\n return x'):
        data = model(source)
        assert data['status']=='VALUE_FLOW_CANDIDATES', data['reason']
    assert any(n.get('mayBeUnbound') for n in data['nodes'])
