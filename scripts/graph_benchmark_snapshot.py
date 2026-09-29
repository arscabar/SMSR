"""Read one committed graph revision without modifying the operating database."""
import sqlite3
from contextlib import closing


def snapshot(database, project):
    with closing(sqlite3.connect(database.resolve().as_uri()+'?mode=ro', uri=True)) as db:
        db.execute('BEGIN')
        revision, = db.execute('SELECT revision FROM graph_projects WHERE project_id=?',
                               (project,)).fetchone()
        args = (project, revision)
        nodes = {r[0] for r in db.execute('SELECT node_id FROM graph_revision_nodes '
                                        'WHERE project_id=? AND revision=?', args)}
        edges = list(db.execute('SELECT source_id,target_id FROM graph_revision_edges '
                               'WHERE project_id=? AND revision=?', args))
    return revision, nodes, edges
