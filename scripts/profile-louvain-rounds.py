"""Count local-move rounds, stopping intentionally after five diagnostic rounds."""
from pathlib import Path
import inspect
import json
import sys
import time

sys.path.insert(0, str(Path.home() / 'AppData/Local/SMSR/graph-runtime/Lib/site-packages'))
import networkx as nx
from networkx.algorithms.community import louvain

started = time.perf_counter()
moves = tiny = rounds = 0
class ProfileComplete(Exception):
    pass

def round_started():
    global moves, tiny, rounds
    if rounds:
        print(json.dumps(dict(round=rounds,moves=moves,tinyPositiveMoves=tiny,
            seconds=time.perf_counter()-started)), flush=True)
    rounds += 1
    moves = tiny = 0
    if rounds > 5:
        raise ProfileComplete()

def moved(gain):
    global moves, tiny
    moves += 1
    tiny += 0 < gain < 1e-16

source = inspect.getsource(louvain._one_level)
needle = '        nb_moves = 0\n'
move = '            if best_com != node2com[u]:\n'
assert source.count(needle) == source.count(move) == 1
source = source.replace(needle, '        round_started()\n' + needle)
source = source.replace(move, move + '                moved(best_mod)\n')
scope = dict(louvain._one_level.__globals__, round_started=round_started,moved=moved)
exec(compile(source, '<smsr-round-profile>', 'exec'), scope)
louvain._one_level = scope['_one_level']
count = 50000
graph = nx.Graph()
graph.add_nodes_from(sorted(f'x{i}' for i in range(count)))
edges = [(f'x{i}',f'x{(i+s)%count}') for i in range(count) for s in (1,2,3,4)]
graph.add_edges_from(sorted(edges, key=lambda e: tuple(sorted(e))))
try:
    nx.community.louvain_communities(graph,seed=42,threshold=1e-4,max_level=10)
except ProfileComplete:
    print('Diagnostic stopped after five rounds; not a product PASS', flush=True)
