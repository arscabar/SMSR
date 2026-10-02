"""Runnable local media boundary/fixture check, no downloads or providers."""
import hashlib
import io
from pathlib import Path
import subprocess
import sys
import tempfile
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'src/SMSR.App/GraphRuntime'))
from PIL import Image
from media_input import run

def main():
    with tempfile.TemporaryDirectory(prefix='smsr-media-check-') as folder:
        root = Path(folder)
        image = root / 'fixture.png'
        Image.new('RGB', (320, 200), '#1565c0').save(image)
        def request(path):
            return dict(root=str(root), path=str(path), hash=hashlib.sha256(path.read_bytes()).hexdigest().upper())
        result = run(request(image))
        assert result['width'] == 320 and result['height'] == 200
        assert result['frames'][0]['location'] == 'image:0,0,320,200'
        stale = request(image); stale['hash'] = 'A' * 64
        try:
            run(stale)
            raise AssertionError('stale source accepted')
        except ValueError:
            pass
        for name, args in [('silent.wav', ['-f', 'lavfi', '-i', 'anullsrc=r=16000:cl=mono', '-t', '1']),
                           ('scene.mp4', ['-f', 'lavfi', '-i', 'color=c=blue:s=320x200:r=2', '-t', '2', '-pix_fmt', 'yuv420p'])]:
            path = root / name
            subprocess.run(['ffmpeg', '-nostdin', '-v', 'error', *args, str(path)], check=True, timeout=30)
            data = run(request(path))
            assert 0 < data['durationSeconds'] < 3
            if name.endswith('.mp4'):
                assert data['status'] == 'FRAMES_AVAILABLE' and 1 <= len(data['frames']) <= 12
                assert all(0 <= f['timeSeconds'] < data['durationSeconds'] for f in data['frames'])
            else:
                assert data['status'] in {'TRANSCRIPTION_UNAVAILABLE', 'NO_SPEECH'}
        print('Media image/frame/time/hash/silence/missing-model checks OK')

if __name__ == '__main__':
    main()
