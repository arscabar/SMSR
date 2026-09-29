"""Trusted JDK compiler only; no repository code, processors or build commands run."""
import json
import os
from pathlib import Path
import struct
import subprocess
from java_control import enrich


def java_path():
    name = 'java.exe' if os.name == 'nt' else 'java'
    # Do not use Windows' implicit current-directory executable search.
    for directory in os.get_exec_path():
        if os.path.isabs(directory) and (Path(directory) / name).is_file():
            return str(Path(directory) / name)
    raise ValueError('JDK executable unavailable')


def encode(files):
    if not isinstance(files, list) or not 1 <= len(files) <= 500:
        raise ValueError('Invalid Java file count')
    chunks = [struct.pack('>i', len(files))]
    for source in files:
        for key in ('path', 'text'):
            value = source[key]
            if not isinstance(value, str):
                raise ValueError('Invalid source field')
            data = value.encode('utf-8')
            if len(data) > (4096 if key == 'path' else 2 * 1024 * 1024):
                raise ValueError('Source field limit exceeded')
            chunks.extend((struct.pack('>i', len(data)), data))
    result = b''.join(chunks)
    if len(result) > 32 * 1024 * 1024:
        raise ValueError('Java input limit exceeded')
    return result


def run(request, classes=None):
    classes = Path(classes) if classes else Path(__file__).resolve().parent.parent / 'JavaAnalysis'
    if not (classes / 'smsr' / 'Main.class').is_file():
        raise ValueError('Java helper unavailable')
    env = {k: v for k, v in os.environ.items() if k.upper() not in
           {'JAVA_TOOL_OPTIONS', 'JDK_JAVA_OPTIONS', '_JAVA_OPTIONS', 'CLASSPATH'}}
    completed = subprocess.run([java_path(), '-Xmx512m', '-XX:-UsePerfData', '-cp', str(classes), 'smsr.Main'],
        input=encode(request['files']), stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
        timeout=100, env=env, creationflags=subprocess.CREATE_NO_WINDOW if os.name == 'nt' else 0)
    if completed.returncode or len(completed.stdout) > 16 * 1024 * 1024:
        raise ValueError('Java compilation analysis failed')
    return enrich(json.loads(completed.stdout))
