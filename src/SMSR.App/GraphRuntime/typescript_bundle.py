"""Compiler facts only. No repository code, configuration, plugins or builds execute."""
import json
import os
from pathlib import Path
import subprocess
from typescript_runtime import runtime
from java_control import enrich


def run(request):
    node, compiler = runtime()
    data = json.dumps({'files': request['files']}, ensure_ascii=True).encode()
    if len(data) > 32 * 1024 * 1024:
        raise ValueError('TypeScript request limit exceeded')
    env = {k: v for k, v in os.environ.items() if k.upper() not in {
        'NODE_OPTIONS', 'NODE_PATH', 'NODE_V8_COVERAGE', 'NODE_COMPILE_CACHE'}}
    env['NODE_DISABLE_COMPILE_CACHE'] = '1'
    completed = subprocess.run([node, '--no-addons', '--max-old-space-size=512',
        str(Path(__file__).with_name('typescript_main.cjs')), compiler], input=data,
        stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, timeout=100, env=env,
        creationflags=subprocess.CREATE_NO_WINDOW if os.name == 'nt' else 0)
    if completed.returncode or len(completed.stdout) > 16 * 1024 * 1024:
        raise ValueError('TypeScript analysis failed; verify compiler 6.0, input and limits')
    return enrich(json.loads(completed.stdout), 'functions', 'TypeScript')
