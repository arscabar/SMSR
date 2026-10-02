"""Grouping/representatives/summary provenance, fixed-source parity and scale."""
from graph_overview import run
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).parent / 'vendor'))
from graphify.cluster import cluster, score_all
import networkx as nx

nodes = [dict(nodeId=f'n{i}',label='int' if i == 0 else f'Component{i}',kind='symbol',sourcePath=f'{i//5}/x.py',line=1) for i in range(11)]
edges = [dict(sourceId=f'n{a}',targetId=f'n{b}',relation='CALLS',resolution='RESOLVED',confidence='EXTRACTED',ownerPath=f'{a//5}/x.py',sourceLine=1)
         for start in (0,5) for a in range(start,start+5) for b in range(a+1,start+5)]
edges.append(dict(edges[0],sourceId='n4',targetId='n5'))
request = dict(nodes=nodes,edges=edges,revision=7)
result = run(request)
assert result == run(request)
assert result['isolatedNodes'] == 1 and result['groups']
assert all(c['label'] != 'int' for c in result['core'])
assert all(0 <= g['cohesion'] <= 1 for g in result['groups'])
assert all(s['edge'] in edges for s in result['surprises'])
assert sum(e['count'] for e in result['links']) > 0
graph = nx.Graph(); graph.add_nodes_from(f'n{i}' for i in range(10)); graph.add_edges_from((e['sourceId'],e['targetId']) for e in edges)
original = cluster(graph)
assert {frozenset(g['memberIds']) for g in result['groups']} == {frozenset(m) for m in original.values()}
assert sorted(g['cohesion'] for g in result['groups']) == sorted(score_all(graph,original).values())
print('Fixed-source groups/cohesion, stable representatives, hub filtering and directed original-edge summaries passed')

if '--large' in sys.argv:
    import time
    nodes = [dict(nodeId=f'x{i}',label=f'Component{i}',kind='symbol',sourcePath='x.py',line=1) for i in range(50000)]
    edges = [dict(sourceId=f'x{i}',targetId=f'x{(i+step)%50000}',relation='CALLS',resolution='RESOLVED',confidence='EXTRACTED',ownerPath='x.py',sourceLine=1) for i in range(50000) for step in (1,2,3,4)]
    start=time.monotonic(); result=run(dict(nodes=nodes,edges=edges,revision=1))
    assert result['scannedNodes']==50000 and result['scannedEdges']==200000
    print('50000 nodes/200000 edges:', round(time.monotonic()-start,2),'seconds;',len(result['groups']),'groups')
