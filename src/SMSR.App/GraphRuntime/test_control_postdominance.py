"""Independent oracle: remove each vertex and search for a surviving exit path."""
from itertools import combinations, product
from control_postdominance import solve


def verify():
    choices = [c for length in (1, 2) for c in combinations(range(4), length)]
    for choice in product(choices, repeat=3):
        next_nodes = dict(enumerate(choice)) | {3: [-1], -1: []}

        def exits(start, removed):
            seen, pending = {removed}, [start]
            while pending:
                node = pending.pop()
                if node in seen:
                    continue
                if node == -1:
                    return True
                seen.add(node)
                pending.extend(next_nodes[node])
            return False

        edges = [dict(source=n, target=t, outcome=str(i))
                 for n in range(4) for i, t in enumerate(next_nodes[n])]
        actual = solve(list(range(4)), edges, lambda n: None, lambda n: None)
        if any(not exits(n, -2) for n in range(4)):
            assert actual['reason'] == 'NON_EXIT_REACHABLE_REGION'
            continue
        def post(n, d):
            return not exits(n, d)
        expected = {(e['source'], e['target'], d, e['outcome']) for e in edges
                    for d in range(4) if post(e['target'], d) and
                    (d == e['source'] or not post(e['source'], d))}
        assert expected == {(e['controller'], e['successor'], e['dependent'], e['outcome'])
                            for e in actual['links']}
        for item in actual['postdominators']:
            n = item['offset']
            strict = [d for d in range(-1, 4) if d != n and post(n, d)]
            nearest = [d for d in strict if all(post(d, other) for other in strict)]
            assert nearest == [item['immediate']]
    def reject(n):
        raise ValueError('test limit')
    for spend, work in ((reject, lambda n: None), (lambda n: None, reject)):
        try:
            solve([0], [dict(source=0, target=-1, outcome='EXIT')], spend, work)
            raise AssertionError('limit missing')
        except ValueError as error:
            assert str(error) == 'test limit'
    print('Control postdominance: 1000 graph oracle passed')


if __name__ == '__main__':
    verify()
