import json
import shutil
import subprocess
import tempfile
from pathlib import Path

def tools(raw):
    probe, ffmpeg = shutil.which('ffprobe'), shutil.which('ffmpeg')
    if not probe or not ffmpeg:
        raise ValueError('Local FFmpeg missing')
    # Isolated input copy prevents container references from resolving beside user files.
    folder = tempfile.TemporaryDirectory(prefix='smsr-media-')
    path = Path(folder.name) / 'input.bin'
    path.write_bytes(raw)
    try:
        reply = subprocess.run([probe, '-v', 'error', '-protocol_whitelist', 'file,pipe', '-format_whitelist', 'mov,matroska,webm,ogg,mp3,wav', '-show_format', '-show_streams', '-of', 'json', str(path)], capture_output=True, timeout=30, check=True)
        info = json.loads(reply.stdout)
        duration = float(info.get('format', {}).get('duration', 0))
        if not 0 < duration <= 1200:
            raise ValueError('Media duration limit: 20 minutes')
        return folder, path, ffmpeg, duration, info
    except Exception:
        folder.cleanup()
        raise

def extract(ffmpeg, path, arguments):
    reply = subprocess.run([ffmpeg, '-nostdin', '-v', 'error', '-protocol_whitelist', 'file,pipe', '-format_whitelist', 'mov,matroska,webm,ogg,mp3,wav', '-i', str(path), *arguments], capture_output=True, timeout=90, check=True)
    if len(reply.stdout) > 40_000_000:
        raise ValueError('Decoded output limit')
    return reply.stdout
