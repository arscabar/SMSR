"""Finite GEN/KILL analysis; integer bitsets keep candidate unions compact."""
from collections import deque


def solve(entry, normal, exceptional, writes, work):
    incoming = {entry: 1}
    pending, queued = deque([entry]), {entry}
    while pending:
        offset = pending.popleft()
        queued.remove(offset)
        before = incoming[offset]
        after = writes.get(offset, before)
        for targets, state in ((normal.get(offset, ()), after),
                               (exceptional.get(offset, ()), before | after)):
            for target in targets:
                work(1 + state.bit_count())
                previous = incoming.get(target, 0)
                combined = previous | state
                if combined != previous:
                    incoming[target] = combined
                    if target not in queued:
                        pending.append(target)
                        queued.add(target)
    return incoming


def successors(function, work):
    normal, exceptional = {}, {}
    for edge in function['edges']:
        normal.setdefault(edge['source'], set()).add(edge['target'])
    for region in function['exceptionRegions']:
        for item in function['instructions']:
            work(1)
            if region['start'] <= item['offset'] < region['end']:
                exceptional.setdefault(item['offset'], set()).add(region['target'])
    return normal, exceptional
