import hashlib
import json
import subprocess
import tempfile
import shutil
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'src/SMSR.App/GraphRuntime'))
from media_input import run

root = Path(__file__).resolve().parents[1]
results = []
for language in ['english', 'korean']:
    path = root / 'artifacts/media-acceptance' / (language+'.wav')
    report = run(dict(root=str(root), path=str(path), hash=hashlib.sha256(path.read_bytes()).hexdigest().upper()))
    segments = report['blocks'][1:]
    assert report['status'] == 'TEXT_AVAILABLE' and segments, language+' transcription missing'
    if language == 'korean':
        assert any('\uac00' <= c <= '\ud7a3' for c in ''.join(s['text'] for s in segments)), 'Korean transcript not recognized'
    results.append(dict(language=language, status=report['status'], engine=report['engine'],
                        duration=report['durationSeconds'], segments=segments))
path = root / 'docs/test-data/media-semantic-flow.png'
image = run(dict(root=str(root), path=str(path), hash=hashlib.sha256(path.read_bytes()).hexdigest().upper()))
assert image['status'] == 'TEXT_AVAILABLE' and any('Store' in b['text'] for b in image['blocks'])
results.append(dict(kind='image', status=image['status'], engine=image['engine'], blocks=image['blocks']))
from PIL import Image
from document_input import run as document_read
from document_preview import run as preview
with tempfile.TemporaryDirectory(prefix='smsr-content-') as folder:
    pdf = Path(folder)/'scan.pdf'
    Image.open(path).convert('RGB').save(pdf,format='PDF',resolution=144)
    request=dict(root=folder,path=str(pdf),hash=hashlib.sha256(pdf.read_bytes()).hexdigest().upper())
    scan=document_read(request)
    assert any('Store' in b['text'] and b['location'].startswith('page:1:image:') for b in scan['blocks'])
    raster=preview(dict(request,page=1))
    assert raster['width']==900 and raster['height']==260
    results.append(dict(kind='scanned-pdf',status=scan['status'],blocks=scan['blocks'],rasterSize=[raster['width'],raster['height']]))
    shutil.copyfile(pdf,root/'artifacts/media-acceptance/scan.pdf')
    video=Path(folder)/'speech.mp4'
    subprocess.run(['ffmpeg','-nostdin','-v','error','-f','lavfi','-i','color=c=blue:s=320x200:r=2',
        '-i',str(root/'artifacts/media-acceptance/english.wav'),'-t',str(results[0]['duration']),'-pix_fmt','yuv420p','-c:a','aac',str(video)],check=True,timeout=30)
    scene=run(dict(root=folder,path=str(video),hash=hashlib.sha256(video.read_bytes()).hexdigest().upper()))
    assert scene['status']=='TEXT_AND_FRAMES' and scene['frames'] and any('evidence' in b['text'] for b in scene['blocks'])
    assert all(0<=f['timeSeconds']<scene['durationSeconds'] for f in scene['frames'])
    results.append(dict(kind='video',status=scene['status'],engine=scene['engine'],frames=len(scene['frames']),blocks=scene['blocks']))
    shutil.copyfile(video,root/'artifacts/media-acceptance/speech.mp4')
print(json.dumps(results, ensure_ascii=True, indent=2))
(root/'docs/test-data/media-content-acceptance-2026-10-01.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')
