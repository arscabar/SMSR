"""Independent unbounded BFS oracle; no application traversal code imported."""
from collections import deque
from math import ceil
from random import Random


def distances(adj, source):
    result, queue = {source: 0}, deque([source])
    while queue:
        node = queue.popleft()
        for target in sorted(adj[node]):
            if target not in result:
                result[target] = result[node] + 1
                queue.append(target)
    return result


def cases(nodes, edges):
    # ponytail: all-source BFS for small repos; sample sources for larger corpora.
    if len(nodes) > 2000 or len(edges) > 10000:
        raise ValueError('Benchmark oracle limited to 2000 nodes / 10000 edges')
    forward = {n: set() for n in nodes}
    reverse = {n: set() for n in nodes}
    for source, target in edges:
        forward[source].add(target)
        reverse[target].add(source)
    positive, negative = [], []
    for source in sorted(nodes):
        reachable = distances(forward, source)
        by_depth = {d: t for t, d in sorted(reachable.items()) if 0 < d <= 8}
        positive.extend((source, t, d) for d, t in by_depth.items())
        missing = next((n for n in sorted(nodes) if n not in reachable), None)
        if missing is not None:
            negative.append((source, missing, None))
    rng = Random(29)
    rng.shuffle(positive)
    rng.shuffle(negative)
    if len(positive) < 32 or len(negative) < 8 or len(nodes) < 40:
        raise ValueError('Need 32 reachable pairs, 8 unreachable pairs and 40 nodes')
    ranked = sorted(nodes, key=lambda n: (-len(reverse[n]), n))
    impacts = ranked[:20] + rng.sample(ranked[20:], 20)
    return positive[:32] + negative[:8], impacts, forward, reverse


def verify_path(result, source, target, distance, edges):
    assert not result['truncated'], 'Path exceeded API traversal bounds'
    assert result['found'] == (distance is not None)
    current = source
    for edge in result['edges']:
        assert edge['sourceId'] == current
        current = edge['targetId']
        assert (edge['sourceId'], current) in edges
    if distance is not None:
        assert current == target and len(result['edges']) == distance
    else:
        assert not result['edges']


def verify_impact(result, source, reverse):
    reachable = distances(reverse, source)
    expected = {n for n, d in reachable.items() if 0 < d <= 8}
    actual = [n['nodeId'] for n in result['nodes']]
    assert len(actual) == len(set(actual)) <= 100
    assert set(actual) <= expected
    limited = len(expected) > 100 or any(d > 8 for d in reachable.values())
    assert result['truncated'] == limited
    if len(expected) <= 100:
        assert set(actual) == expected
    else:
        assert len(actual) == 100


def stats(values):
    ordered = sorted(values)
    return dict(samples=len(values), p95Ms=ordered[ceil(len(values)*.95)-1],
                maxMs=max(values), totalMs=sum(values))
