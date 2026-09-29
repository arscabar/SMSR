"""Finite-exit postdominance. No invented exits for non-terminating regions."""
from collections import defaultdict


def reachable(start, next_nodes, work):
    seen, pending = set(), [start]
    while pending:
        node = pending.pop()
        work(1)
        if node not in seen:
            seen.add(node)
            pending.extend(next_nodes.get(node, ()))
    return seen


def solve(nodes, edges, spend, work):
    next_nodes, previous = defaultdict(set), defaultdict(set)
    for e in edges:
        work(1)
        next_nodes[e['source']].add(e['target'])
        previous[e['target']].add(e['source'])
    if not set(nodes) <= reachable(-1, previous, work):
        return dict(status='UNAVAILABLE', reason='NON_EXIT_REACHABLE_REGION',
                    postdominators=[], links=[])
    ids = [-1, *nodes]
    width = (len(ids)+63)//64
    work(len(ids)*width)
    bits = {n: 1 << i for i, n in enumerate(ids)}
    sets = {n: (1 << len(ids))-1 for n in ids}
    sets[-1] = 1
    # ponytail: bounded bitset fixed point; tree algorithm if large-CFG profiling warrants it.
    changed = True
    while changed:
        changed = False
        for node in reversed(nodes):
            value = (1 << len(ids))-1
            for child in next_nodes[node]:
                work(width)
                value &= sets[child]
            value |= bits[node]
            if value != sets[node]:
                sets[node] = value
                changed = True

    def members(mask):
        while mask:
            bit = mask & -mask
            work(1)
            yield ids[bit.bit_length()-1]
            mask ^= bit

    immediate, links = [], []
    def depth(node):
        work(width)
        return sets[node].bit_count()
    for n in nodes:
        strict = sets[n] & ~bits[n]
        nearest = max(members(strict), key=depth)
        spend(1)
        immediate.append(dict(offset=n, immediate=nearest))
    for e in edges:
        mask = sets[e['target']] & (~sets[e['source']] | bits[e['source']]) & ~1
        for dependent in members(mask):
            spend(1)
            links.append(dict(controller=e['source'], successor=e['target'],
                              dependent=dependent, outcome=e['outcome']))
    return dict(status='NORMAL_CFG_CONTROL_DEPENDENCE', reason=None,
                postdominators=immediate, links=links)
