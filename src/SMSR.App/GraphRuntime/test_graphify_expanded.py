"""Three-file identity/relationship oracle for each of the 16 grammars."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
from graphify_index import run
from language_cases import CASES

raw=subprocess.run([sys.argv[1],str(Path(__file__).with_name('graphify_expanded_oracle.py'))],
    capture_output=True,text=True,encoding='utf-8',timeout=90,check=True)
oracle=json.loads(raw.stdout)
for language,(extension,source) in CASES.items():
    files=[dict(path=f'{prefix}/sample.{extension}',text=source,
        hash=hashlib.sha256(source.encode()).hexdigest().upper()) for prefix in ('a','b','c')]
    facts=run(dict(files=files))
    signature=dict(nodes=sorted((n['nodeId'],n['sourcePath'],n['line'],n['label']) for n in facts['nodes']),
        edges=sorted(tuple(e[k] for k in ('sourceId','targetId','relation','ownerPath','sourceLine','resolution','confidence')) for e in facts['edges']))
    assert json.loads(json.dumps(signature))==oracle[language],language
    assert {n['ownerPath'] for n in facts['nodes']}=={f['path'] for f in files},language
    print(language+': three-file original IDs/owners/relations match')
print('48-file duplicate-symbol original oracle passed; not compiler precision')
