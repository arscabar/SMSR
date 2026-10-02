"""Every additional fixture retains original declarations and endpoint direction."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
from graphify_index import run
from language_extension_cases import CASES

raw = subprocess.run([sys.executable, str(Path(__file__).with_name('graphify_languages_oracle.py'))],
    capture_output=True, text=True, encoding='utf-8', check=True, timeout=90)
oracle = json.loads(raw.stdout)
for extension, source in CASES.items():
    facts = run(dict(files=[dict(path='sample'+extension, text=source,
        hash=hashlib.sha256(source.encode()).hexdigest().upper())]))
    assert set(oracle[extension]['nodes']) <= {n['nodeId'] for n in facts['nodes']}, extension
    assert {tuple(e) for e in oracle[extension]['edges']} <= {
        (e['sourceId'], e['targetId'], e['relation']) for e in facts['edges']}, extension
print('26 added formats retain fixed-source declarations/relationships; not whole-language precision')
