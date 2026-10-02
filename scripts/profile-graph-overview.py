"""Standalone bounded-fixture phase timings; never reads project source data."""
from pathlib import Path
import functools
import importlib
import json
import sys
import time

runtime = Path(__file__).resolve().parents[1] / 'src/SMSR.App/GraphRuntime'
sys.path[:0] = [str(runtime), str(runtime / 'vendor')]
from graph_overview import run

started = time.perf_counter()
totals = {}

def emit(phase, event, **fields):
    print(json.dumps(dict(phase=phase, event=event,
        elapsedSeconds=round(time.perf_counter()-started, 4), **fields)), flush=True)

def measure(module, name):
    original = getattr(module, name)
    @functools.wraps(original)
    def wrapped(*args, **kwargs):
        graph = args[0]
        emit(name, 'start', nodes=graph.number_of_nodes(), edges=graph.number_of_edges())
        begin = time.perf_counter()
        try:
            return original(*args, **kwargs)
        finally:
            seconds = time.perf_counter()-begin
            totals[name] = totals.get(name, 0)+seconds
            emit(name, 'end', seconds=round(seconds, 4))
    setattr(module, name, wrapped)

cluster = importlib.import_module('graphify.cluster')
for name in ('cluster', '_partition', '_split_community', 'score_all', 'cohesion_score'):
    measure(cluster, name)
import networkx as nx
measure(nx.community, 'louvain_communities')
measure(importlib.import_module('graphify.analyze'), 'god_nodes')

count = int(sys.argv[1]) if len(sys.argv) > 1 else 50000
if not 1 <= count <= 50000:
    raise ValueError('Fixture node limit')
emit('input', 'start')
nodes = [dict(nodeId=f'x{i}',label=f'Component{i}',kind='symbol',sourcePath='x.py') for i in range(count)]
edges = [dict(sourceId=f'x{i}',targetId=f'x{(i+step)%count}',relation='CALLS',
    resolution='RESOLVED',confidence='EXTRACTED',ownerPath='x.py',sourceLine=1)
    for i in range(count) for step in (1,2,3,4)]
request = dict(revision=1,nodes=nodes,edges=edges)
emit('input', 'end', nodes=count, edges=len(edges), bytes=len(json.dumps(request).encode()))
emit('overview', 'start')
result = run(request)
emit('overview', 'end', groups=len(result['groups']), totals=totals)
assert result['scannedNodes'] == count and result['scannedEdges'] == count*4
members = [n for g in result['groups'] for n in g['memberIds']]
assert len(members) == count and len(set(members)) == count
emit('serialization', 'start')
emit('serialization', 'end', bytes=len(json.dumps(result).encode()))
