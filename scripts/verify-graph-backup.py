"""Back up live SMSR with SQLite's snapshot API and restore only to a new test file."""
import json
import os
import sqlite3
from datetime import datetime, timezone
from pathlib import Path

source = Path(os.environ['LOCALAPPDATA']) / 'SMSR' / 'smsr.db'
if not source.is_file():
    raise SystemExit('SMSR database not found')
folder = source.parent / 'verification-backups' / datetime.now(timezone.utc).strftime('%Y%m%d-%H%M%S-%f')
folder.mkdir(parents=True, exist_ok=False)
backup_path, restored_path = folder/'snapshot.db', folder/'restored.db'
with sqlite3.connect(source.as_uri()+'?mode=ro', uri=True) as live, sqlite3.connect(backup_path) as backup:
    live.backup(backup)
with sqlite3.connect(backup_path) as backup, sqlite3.connect(restored_path) as restored:
    backup.backup(restored)
    tables = [row[0] for row in backup.execute("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")]
    counts = {}
    for table in tables:
        safe = '"' + table.replace('"','""') + '"'
        expected = backup.execute('SELECT count(*) FROM '+safe).fetchone()[0]
        assert expected == restored.execute('SELECT count(*) FROM '+safe).fetchone()[0], table
        counts[table] = expected
    assert restored.execute('PRAGMA integrity_check').fetchone() == ('ok',)
    assert not restored.execute('PRAGMA foreign_key_check').fetchall()
print(json.dumps(dict(backup=str(backup_path), restored=str(restored_path), integrity='ok', counts=counts)))
