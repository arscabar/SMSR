"""Read-only inventory of parser diagnostics; never copy source bodies to logs."""
import json
import hashlib
import sqlite3
import sys
import traceback
from pathlib import Path
from tree_sitter_language_pack import get_parser


def inventory(database, root, project, mode='original'):
    with sqlite3.connect(Path(database).resolve().as_uri() + '?mode=ro', uri=True) as db:
        rows = db.execute('SELECT owner_path FROM graph_issues WHERE project_id=? '
                          'AND relation=? ORDER BY owner_path', (project, 'PARSE_ERROR')).fetchall()
    root = Path(root).resolve()
    for (relative,) in rows:
        path = (root / relative).resolve()
        if not path.is_relative_to(root):
            raise ValueError('Diagnostic path outside project')
        raw = path.read_bytes()
        try:
            text = raw.decode('utf-8-sig')
        except UnicodeDecodeError:
            text = raw.decode('cp949')
        parser = get_parser('csharp' if path.suffix == '.cs' else 'cpp')
        tree = parser.parse(text.encode())
        stack, errors = [tree.root_node], []
        while stack:
            node = stack.pop()
            if node.type == 'ERROR' or node.is_missing:
                errors.append(dict(type=str(node.type), line=int(node.start_point[0]) + 1,
                                   endLine=int(node.end_point[0]) + 1))
            stack.extend(reversed(node.children))
        record = dict(path=relative, count=len(errors), errors=errors[:8])
        if mode == 'recovered':
            sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'src/SMSR.App/GraphRuntime'))
            from graphify_index import run
            facts = run(dict(files=[dict(path=relative, text=text,
                hash=hashlib.sha256(raw).hexdigest().upper(),
                contentHash=hashlib.sha256(text.encode()).hexdigest().upper())]))
            record.update(symbols=len(facts['nodes']),
                          remaining=[i for i in facts['issues'] if i['relation'] == 'PARSE_ERROR'],
                          recovered=len([i for i in facts['issues'] if i['relation'].endswith('RECOVERY')]))
        print(json.dumps(record, ensure_ascii=False))


if __name__ == '__main__':
    try:
        inventory(*sys.argv[1:])
    except Exception as error:
        print(json.dumps(dict(failed=type(error).__name__, reason=str(error),
                              frames=[(f.name, f.lineno) for f in traceback.extract_tb(error.__traceback__)]), ensure_ascii=True))
        raise SystemExit(1)
