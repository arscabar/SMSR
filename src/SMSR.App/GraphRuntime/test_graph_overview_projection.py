"""No-isolate fast path preserves fixed-source memberships, scores and facts."""
from pathlib import Path
import sys
import networkx as nx
from graph_overview import run
sys.path.insert(0, str(Path(__file__).parent / 'vendor'))
from graphify.cluster import cluster, score_all

for shape in (nx.complete_graph(12), nx.barbell_graph(8,4),
              nx.connected_watts_strogatz_graph(60,6,.2,seed=42),
              nx.path_graph(16)):
    graph = nx.relabel_nodes(shape, lambda n: f'n{n}')
    request = dict(revision=1,
        nodes=[dict(nodeId=n,label=n,kind='symbol',sourcePath='fixture.py') for n in graph],
        edges=[dict(sourceId=a,targetId=b,relation='CALLS',resolution='RESOLVED',
            confidence='EXTRACTED',ownerPath='fixture.py',sourceLine=1) for a,b in graph.edges])
    result = run(request)
    original = cluster(graph.copy())
    assert [g['memberIds'] for g in result['groups']] == list(original.values())
    assert [g['cohesion'] for g in result['groups']] == list(score_all(graph,original).values())
    assert result == run(request)
    assert result['isolatedNodes'] == 0
    assert result['scannedNodes'] == len(graph)
    assert result['scannedEdges'] == graph.number_of_edges()
    assert all(s['edge'] in request['edges'] for s in result['surprises'])
    assert len({n for g in result['groups'] for n in g['memberIds']}) == len(graph)
print('No-isolate projection: fixed-source groups/cohesion, determinism and full membership passed')
