"""Normal AST CFG, with finite-exit control dependence shared with CPython."""
from collections import defaultdict
from control_postdominance import reachable, solve


def enrich(report, key='methods', language='Java'):
    remaining = [250000, 20000]

    def work(count):
        remaining[0] -= count
        if remaining[0] < 0:
            raise ValueError(language+' control work limit')

    def spend(count):
        remaining[1] -= count
        if remaining[1] < 0:
            raise ValueError(language+' control output limit')

    for method in report[key]:
        control = method['control']
        control.update(postdominators=[], links=[], boundaries=[
            'AST_NORMAL_FLOW', 'TERMINATING_PATH_POSTDOMINANCE',
            'IMPLICIT_EXCEPTIONS_AND_CALLEE_EFFECTS_UNMODELED',
            'PATH_CONDITIONS_UNMODELED_EXCEPT_BOOLEAN_LITERALS',
            'NO_INTERPROCEDURAL_CONTROL_OR_TAINT'])
        if control['status'] == 'UNAVAILABLE':
            continue
        adjacent = defaultdict(list)
        for edge in control['transfers']:
            work(1)
            adjacent[edge['source']].append(edge['target'])
        live = reachable(control['entry'], adjacent, work) - {-1}
        nodes = [n for n in control['nodes'] if n['id'] in live]
        edges = [e for e in control['transfers'] if e['source'] in live]
        control.update(solve([n['id'] for n in nodes], edges, spend, work))
        control['nodes'] = nodes
        control['transfers'] = edges
    return report
