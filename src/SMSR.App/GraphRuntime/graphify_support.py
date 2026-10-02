"""Upstream dispatch is the source of truth; missing optional engines stay explicit."""
from pathlib import Path
import importlib.util

VERSION = '0.9.73'
DOCUMENTS = {'.md', '.mdx', '.qmd', '.skill'}
FILE_ONLY = {'.dm': 'tree-sitter-dm', '.dme': 'tree-sitter-dm', '.dmi': 'binary-icon'}
OPTIONAL = {'.groovy': 'tree_sitter_groovy', '.gradle': 'tree_sitter_groovy',
            '.zig': 'tree_sitter_zig',
            '.vb': 'tree_sitter_vb_dotnet', '.robot': 'robot', '.resource': 'robot'}

def unavailable(ext):
    if ext in FILE_ONLY:
        return FILE_ONLY[ext]
    dependency = OPTIONAL.get(ext)
    return dependency if dependency and importlib.util.find_spec(dependency) is None else None

def supported(module, path):
    ext = Path(path).suffix.lower()
    return ext in module._DISPATCH and ext not in DOCUMENTS and not unavailable(ext)

def missing(known):
    for item in known.values():
        ext = Path(item['path']).suffix.lower()
        reason = unavailable(ext)
        if reason:
            yield dict(ownerPath=item['path'], sourceLine=1, relation='STRUCTURE_UNSUPPORTED',
                reason='파일 등록만 지원; 추가 분석기 필요: ' + reason, candidatePaths=[])
