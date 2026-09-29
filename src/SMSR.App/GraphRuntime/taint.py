from collections import defaultdict, deque

SOURCES = {'input', 'readline', 'request', 'req', 'argv', 'querystring', 'getparameter'}
SINKS = {'eval', 'exec', 'system', 'popen', 'execute', 'executescript', 'start', 'writealltext'}

def findings(statements, edges):
    graph = defaultdict(list)
    for edge in edges:
        if edge['relation'] == 'DATA_MAY_DEPEND':
            graph[edge['source']].append(edge['target'])
    source_ids = [s['id'] for s in statements if SOURCES.intersection(x.lower() for x in s['uses'])]
    sinks = {s['id'] for s in statements if SINKS.intersection(x.lower() for x in s['uses'])}
    results = []
    for source in source_ids:
        queue, previous = deque([source]), {source:None}
        while queue:
            current = queue.popleft()
            if current in sinks:
                path, cursor = [], current
                while cursor is not None:
                    path.append(cursor)
                    cursor = previous[cursor]
                results.append(dict(source=source, sink=current, path=list(reversed(path)), confidence='HEURISTIC_REVIEW', rule='lexical-input-to-sensitive-call', note='Names are not type-resolved. Sanitizers are not trusted. No finding does not mean safe.'))
                if len(results) >= 100:
                    return results
            for target in graph[current]:
                if target not in previous:
                    previous[target] = current
                    queue.append(target)
    return results
