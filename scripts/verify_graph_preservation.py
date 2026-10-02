"""Compare graph facts before/after deployment, read-only; no prompts or rows printed."""
import hashlib
import json
import os
import sqlite3
import sys
from contextlib import closing
from pathlib import Path

TABLES = ('graph_projects', 'graph_files', 'graph_nodes', 'graph_edges', 'graph_issues',
          'graph_revision_nodes', 'graph_revision_edges', 'graph_hyperedges',
          'graph_revision_hyperedges', 'graph_index_formats')


def fingerprint(db, table):
    keys = [row[1] for row in sorted(db.execute(f'PRAGMA table_info("{table}")'), key=lambda r: r[5]) if row[5]]
    order = ','.join('"' + key.replace('"', '""') + '"' for key in keys) or 'rowid'
    digest, count = hashlib.sha256(), 0
    for row in db.execute(f'SELECT * FROM "{table}" ORDER BY {order}'):
        digest.update(json.dumps(row, ensure_ascii=True, separators=(',', ':')).encode())
        digest.update(b'\n')
        count += 1
    return count, digest.hexdigest()


backup = Path(sys.argv[1]).resolve()
live = Path(os.environ['LOCALAPPDATA']) / 'SMSR' / 'smsr.db'
with closing(sqlite3.connect(backup.as_uri() + '?mode=ro', uri=True)) as old, \
        closing(sqlite3.connect(live.as_uri() + '?mode=ro', uri=True)) as current:
    old.execute('BEGIN'); current.execute('BEGIN')
    counts = {}
    for table in TABLES:
        expected = fingerprint(old, table)
        assert expected == fingerprint(current, table), f'Graph facts changed: {table}'
        counts[table] = expected[0]
    assert current.execute('PRAGMA quick_check').fetchone() == ('ok',)
print(json.dumps({'preserved': True, 'tables': counts}))
