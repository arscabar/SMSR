"""Independent state-path exploration of small graphs, including cycles."""
import random
from python_reaching import solve


def verify():
    rng=random.Random(7129)
    for _ in range(500):
        count=rng.randrange(2,9)
        normal={i:{j for j in range(count) if rng.random()<0.18} for i in range(count)}
        exceptional={i:{j for j in range(count) if rng.random()<0.08} for i in range(count)}
        writes={i:1 << (i+1) for i in range(count) if rng.random()<0.4}
        expected, seen, stack={}, set(), [(0,1)]
        while stack:
            node, definition=stack.pop()
            if (node,definition) in seen: continue
            seen.add((node,definition))
            expected[node]=expected.get(node,0)|definition
            after=writes.get(node,definition)
            stack.extend((target,after) for target in normal[node])
            for target in exceptional[node]:
                stack.extend(((target,definition),(target,after)))
        assert solve(0,normal,exceptional,writes,lambda n:None)==expected
