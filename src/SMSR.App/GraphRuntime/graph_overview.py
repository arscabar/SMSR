"""Fixed Graphify community/cohesion/god-node analysis over existing graph facts."""
from pathlib import Path
import importlib.util
import sys
import networkx as nx
from graphify_entity import safe_text

def run(request):
    sys.path.insert(0, str(Path(__file__).parent / 'vendor'))
    from graphify.cluster import cluster, score_all
    from graphify.analyze import god_nodes, _BUILTIN_NOISE_LABELS, _is_json_key_node
    nodes, edges = request['nodes'], request['edges']
    if len(nodes) > 50000 or len(edges) > 200000:
        raise ValueError('Overview projection limit')
    graph = nx.Graph()
    for n in sorted(nodes, key=lambda n: n['nodeId']):
        graph.add_node(n['nodeId'], label=safe_text(n['label']), source_file=n['sourcePath'], kind=n['kind'])
    for e in edges:
        a, b = e['sourceId'], e['targetId']
        if a not in graph or b not in graph:
            raise ValueError('Dangling overview endpoint')
        if a != b and e['resolution'] == 'RESOLVED' and e['confidence'] in {'EXTRACTED', 'EXPLICIT'}:
            graph.add_edge(a, b)
    isolates = list(nx.isolates(graph))
    # The common no-isolate case already is the connected projection.
    groups = cluster(graph if not isolates else graph.subgraph(set(graph) - set(isolates)).copy())
    scores = score_all(graph, groups)
    membership = {n: key for key, members in groups.items() for n in members}
    rows = []
    for key, members in groups.items():
        ranked = sorted(members, key=lambda n: (-graph.degree(n), n))
        meaningful = [n for n in ranked if graph.nodes[n]['kind'] in {'symbol','concept','requirement','rationale'}
                      and graph.nodes[n]['label'] not in _BUILTIN_NOISE_LABELS and not _is_json_key_node(graph, n)]
        representatives = meaningful[:5] or ranked[:5]
        rows.append(dict(id=key, name=graph.nodes[representatives[0]]['label'][:128],
            nodeCount=len(members), cohesion=scores[key], memberIds=sorted(members),
            representatives=[dict(nodeId=n, label=graph.nodes[n]['label'][:128], degree=graph.degree(n)) for n in representatives]))
    from graph_overview_links import summarize
    links, surprises, questions = summarize(edges, graph, membership, rows)
    core = [dict(nodeId=n['id'], label=n['label'], degree=n['degree'], reason='연결도가 높은 실제 구성요소') for n in god_nodes(graph)]
    engine = 'LEIDEN_OR_FALLBACK' if importlib.util.find_spec('graspologic_native') or importlib.util.find_spec('graspologic') else 'LOUVAIN'
    return dict(revision=request['revision'], algorithm='Graphify 0.9.73 ' + engine,
        groups=rows, links=links[:1000], core=core, surprises=surprises, questions=questions,
        isolatedNodes=len(isolates), scannedNodes=len(nodes), scannedEdges=len(edges),
        linksTruncated=len(links)>1000, limitation='무방향 구조 그룹; 방향·근거는 기존 관계로 보존. 대표 이름은 결정적 라벨이며 의미 요약은 아니다.')
