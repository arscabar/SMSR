"""Run directly with Python; assertions deliberately stay enabled."""
from graph_benchmark_oracle import cases, distances, stats, verify_impact, verify_path
from graph_benchmark_snapshot import snapshot
from pathlib import Path
from tempfile import TemporaryDirectory
import sqlite3
from contextlib import closing


def rejects(action):
    try:
        action()
    except (AssertionError, ValueError):
        return
    raise AssertionError('Invalid result accepted')


adj = {'a': {'b', 'c'}, 'b': {'c'}, 'c': {'a'}, 'd': set()}
assert distances(adj, 'a') == {'a': 0, 'b': 1, 'c': 1}
edges = {('a', 'b'), ('b', 'c'), ('a', 'c'), ('c', 'a')}
good = dict(found=True, truncated=False, edges=[dict(sourceId='a', targetId='c')])
verify_path(good, 'a', 'c', 1, edges)
verify_path(dict(found=False, truncated=False, edges=[]), 'a', 'd', None, edges)
rejects(lambda: verify_path(good, 'a', 'b', 1, edges))
rejects(lambda: verify_path(good, 'a', 'c', 2, edges))
rejects(lambda: verify_path(dict(good, truncated=True), 'a', 'c', 1, edges))
impact = dict(nodes=[dict(nodeId='b'), dict(nodeId='c')], truncated=False)
verify_impact(impact, 'a', adj)
rejects(lambda: verify_impact(dict(impact, nodes=[dict(nodeId='b')]), 'a', adj))
rejects(lambda: verify_impact(dict(impact, nodes=impact['nodes']*2), 'a', adj))
fan = {'root': {str(i) for i in range(101)}, **{str(i): set() for i in range(101)}}
verify_impact(dict(nodes=[dict(nodeId=str(i)) for i in range(100)], truncated=True), 'root', fan)
assert stats(list(range(1, 41)))['p95Ms'] == 38
nodes = {str(i) for i in range(50)}
edges = [(str(i), str(i+1)) for i in range(49) if i % 5 != 4]
first = cases(nodes, edges)
assert first == cases(nodes, edges)
assert len(first[0]) == len(first[1]) == len(set(first[1])) == 40
assert sum(d is None for _, _, d in first[0]) == 8
rejects(lambda: cases(set(), []))
rejects(lambda: cases(set(range(2001)), []))
with TemporaryDirectory() as folder:
    database = Path(folder)/'test.db'
    with closing(sqlite3.connect(database)) as db:
        db.executescript('''
            CREATE TABLE graph_projects(project_id, revision);
            CREATE TABLE graph_revision_nodes(project_id, revision, node_id);
            CREATE TABLE graph_revision_edges(project_id, revision, source_id, target_id);
            INSERT INTO graph_projects VALUES ('p',2);
            INSERT INTO graph_revision_nodes VALUES ('p',1,'old'),('p',2,'a'),('p',2,'b');
            INSERT INTO graph_revision_edges VALUES ('p',2,'a','b');
        ''')
    assert snapshot(database, 'p') == (2, {'a','b'}, [('a','b')])
print('Benchmark oracle checks passed')
