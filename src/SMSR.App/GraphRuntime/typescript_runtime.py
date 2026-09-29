"""Discover installed runtimes, never project packages or implicit current directory."""
import os
from pathlib import Path
import re


def runtime():
    node = next((Path(p) / 'node.exe' for p in os.get_exec_path()
                 if os.path.isabs(p) and (Path(p) / 'node.exe').is_file()), None)
    if node is None:
        raise ValueError('Installed Node runtime unavailable')
    roots = [Path(os.environ.get('LOCALAPPDATA', '')) / 'Programs/Microsoft VS Code',
             Path(os.environ.get('ProgramFiles', '')) / 'Microsoft VS Code']
    for root in roots:
        if not root.is_absolute():
            continue
        launcher = root / 'bin/code.cmd'
        # Read launcher as data only. Current VS Code uses a versioned directory.
        text = launcher.read_text(encoding='utf-8') if launcher.is_file() else ''
        versions = re.findall(r'\.\.\\([a-f0-9]{10})\\resources\\app', text)
        for base in [root / v for v in versions] + [root]:
            compiler = base / 'resources/app/extensions/node_modules/typescript/lib/typescript.js'
            if compiler.is_file():
                return str(node), str(compiler)
    raise ValueError('Installed VS Code TypeScript 6.0 compiler unavailable')
