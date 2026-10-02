"""Read-only, bounded document extraction; no macros, links, scripts or model calls."""
import hashlib
from pathlib import Path
from graphify_entity import SECRET

def validated(request):
    root, path = Path(request['root']).resolve(), Path(request['path'])
    if not path.is_absolute() or path.is_symlink() or not path.resolve().is_relative_to(root):
        raise ValueError('Document outside source root')
    for parent in path.parents:
        if parent == root:
            break
        if parent.is_symlink() or parent.is_junction():
            raise ValueError('Linked document parent')
    if not path.is_file() or path.stat().st_size > 20_000_000:
        raise ValueError('Document size/missing limit')
    raw = path.read_bytes()
    digest = hashlib.sha256(raw).hexdigest().upper()
    if digest != request['hash']:
        raise ValueError('Stale document')
    return path, raw, digest

def run(request):
    path, raw, digest = validated(request)
    if path.suffix.lower() == '.pdf':
        from document_pdf import read
    elif path.suffix.lower() in {'.docx', '.xlsx'}:
        from document_office import read
    elif path.suffix.lower() == '.html':
        from document_html import read
    else:
        raise ValueError('Unsupported document')
    blocks, size, excluded = [], 0, 0
    for location, text in read(raw, path.suffix.lower()):
        text = str(text).strip()
        if not text:
            continue
        if SECRET.search(text):
            excluded += 1
            continue
        size += len(text)
        if len(text) > 32000 or size > 1_000_000 or len(blocks) >= 5000:
            raise ValueError('Document output limit')
        blocks.append(dict(location=location, text=text, line=len(blocks)+1))
    if hashlib.sha256(path.read_bytes()).hexdigest().upper() != digest:
        raise ValueError('Document changed during extraction')
    has_text = any(not (':image:0,0,' in b['location'] and b['text'].startswith('Image ')) for b in blocks)
    return dict(sourceHash=digest, extractorVersion='document-v1', blocks=blocks,
                status='TEXT_AVAILABLE' if has_text else 'NO_TEXT_OR_SCAN', excludedBlocks=excluded)
