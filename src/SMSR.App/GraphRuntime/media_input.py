"""Bounded local media inputs. Never fetch models or send content to a provider."""
import hashlib
from pathlib import Path
from graphify_entity import SECRET

def run(request):
    root, path = Path(request['root']).resolve(), Path(request['path'])
    if not path.is_absolute() or path.is_symlink() or not path.resolve().is_relative_to(root):
        raise ValueError('Media outside source root')
    if any(p.is_symlink() or p.is_junction() for p in path.parents if p != root and p.is_relative_to(root)):
        raise ValueError('Linked media parent')
    if not path.is_file() or path.stat().st_size > 100_000_000:
        raise ValueError('Media size/missing limit')
    raw = path.read_bytes()
    digest = hashlib.sha256(raw).hexdigest().upper()
    if digest != request['hash']:
        raise ValueError('Stale media')
    suffix = path.suffix.lower()
    if suffix in {'.png', '.jpg', '.jpeg', '.webp', '.bmp', '.gif'}:
        if len(raw) > 20_000_000:
            raise ValueError('Image size limit')
        from media_image import read
    elif suffix in {'.mp3', '.wav', '.ogg', '.m4a'}:
        from media_audio import read
    elif suffix in {'.mp4', '.webm', '.ogv'}:
        from media_video import read
    else:
        raise ValueError('Unsupported media analysis')
    result = read(raw, request)
    blocks, excluded, size = [], 0, 0
    for location, text in result.pop('texts'):
        text = str(text).strip()
        if not text:
            continue
        if SECRET.search(text):
            excluded += 1
            continue
        size += len(text)
        if len(text) > 32000 or size > 500000 or len(blocks) >= 2000:
            raise ValueError('Media output limit')
        blocks.append(dict(location=location, text=text, line=len(blocks)+1))
    result.update(sourceHash=digest, extractorVersion='media-v1', blocks=blocks, excludedBlocks=excluded)
    if hashlib.sha256(path.read_bytes()).hexdigest().upper() != digest:
        raise ValueError('Media changed during analysis')
    return result
