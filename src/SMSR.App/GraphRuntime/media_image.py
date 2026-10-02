import base64
import io
import subprocess
import tempfile
from pathlib import Path
from media_tools import tool, tessdata

def read(raw, request):
    from PIL import Image, ImageStat
    Image.MAX_IMAGE_PIXELS = 16_000_000
    with Image.open(io.BytesIO(raw)) as source:
        if source.width * source.height > 16_000_000 or min(source.size) < 1:
            raise ValueError('Image dimensions limit')
        source.load()
        width, height = source.size
        image = source.convert('RGB')
    texts = [('image:0,0,%d,%d' % (width, height), 'Image %d × %d pixels' % (width, height))]
    status, engine = 'OCR_UNAVAILABLE', 'Pillow'
    ocr = tool('tesseract')
    if ocr:
        with tempfile.TemporaryDirectory(prefix='smsr-ocr-') as folder:
            target = Path(folder) / 'input.png'
            image.save(target)
            data = tessdata()
            languages = 'eng+kor' if data else 'eng'
            arguments = [ocr, str(target), 'stdout'] + (['--tessdata-dir', str(data)] if data else [])
            for attempt in range(2):
                # Explicit renderer works with model-only tessdata folders (no configs/tsv).
                reply = subprocess.run([*arguments, '-l', languages, '--psm', '11', '-c', 'tessedit_create_tsv=1'], capture_output=True, timeout=60, check=True)
                for line in reply.stdout.decode('utf-8', errors='strict').splitlines()[1:]:
                    fields = line.split('\t', 11)
                    if len(fields) == 12 and any(c.isalnum() for c in fields[11]) and float(fields[10]) >= 30:
                        x, y, w, h = map(int, fields[6:10])
                        texts.append((f'image:{x},{y},{w},{h}', fields[11].strip()))
                        if len(texts) > 2000:
                            raise ValueError('OCR block limit')
                if len(texts) > 1 or ImageStat.Stat(image.convert('L')).mean[0] >= 127:
                    break
                # Bounded fallback for bright labels on dark diagrams; original pixel coordinates remain unchanged.
                image.convert('L').point(lambda v: 0 if v > 225 else 255).save(target)
        status, engine = 'TEXT_AVAILABLE' if len(texts) > 1 else 'NO_TEXT', 'Tesseract:'+languages
    image.thumbnail((1024, 1024))
    buffer = io.BytesIO()
    image.save(buffer, format='JPEG', quality=80)
    return dict(texts=texts, status=status, engine=engine, width=width, height=height,
                frames=[dict(location=f'image:0,0,{width},{height}', timeSeconds=None, mime='image/jpeg', data=base64.b64encode(buffer.getvalue()).decode())])
