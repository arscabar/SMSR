import csv
import tempfile
from pathlib import Path
import kuzu
from cypher_guard import validate

def run(request):
    query = validate(request['query'])
    nodes, edges = request['nodes'], request['edges']
    if len(nodes) > 50000 or len(edges) > 200000:
        raise ValueError('Cypher projection exceeds 50000 nodes / 200000 edges')
    with tempfile.TemporaryDirectory(prefix='smsr-cypher-') as temp:
        root = Path(temp)
        with (root / 'nodes.csv').open('w', newline='', encoding='utf-8') as f:
            csv.writer(f).writerows((n['nodeId'], n['label'], n['kind'], n['sourcePath'], n['line']) for n in nodes)
        with (root / 'edges.csv').open('w', newline='', encoding='utf-8') as f:
            csv.writer(f).writerows((e['sourceId'], e['targetId'], e['relation'], e['confidence']) for e in edges)
        options = dict(buffer_pool_size=256*1024*1024, max_num_threads=2, max_db_size=2*1024**3)
        db = kuzu.Database(root / 'graph', **options)
        conn = kuzu.Connection(db)
        try:
            conn.execute('CREATE NODE TABLE Node(id STRING, label STRING, kind STRING, path STRING, line INT64, PRIMARY KEY(id))').close()
            conn.execute('CREATE REL TABLE REL(FROM Node TO Node, relation STRING, confidence STRING)').close()
            for table, name, rows in [('Node', 'nodes', nodes), ('REL', 'edges', edges)]:
                if rows:
                    conn.execute(f"COPY {table} FROM '{(root / (name+'.csv')).as_posix()}' (HEADER=false, DELIM=',', QUOTE='\"', ESCAPE='\"', PARALLEL=false)").close()
        finally:
            conn.close()
            db.close()
        db = kuzu.Database(root / 'graph', read_only=True, **options)
        conn = kuzu.Connection(db)
        try:
            conn.set_query_timeout(5000)
            result = conn.execute(query, request.get('parameters', {}))
            rows = []
            while result.has_next() and len(rows) < 200:
                rows.append(result.get_next())
            reply = dict(columns=result.get_column_names(), rows=rows, truncated=result.has_next(), dialect='Kuzu 0.11.3 read-only', revision=request['revision'])
            result.close()
            return reply
        finally:
            conn.close()
            db.close()
