import os
from pathlib import Path
import shutil

def tool(name):
    command = shutil.which(name)
    if command:
        return command
    if name == 'tesseract':
        for root in (Path(os.environ.get('LOCALAPPDATA', '')) / 'SMSR/tesseract',
                     Path(os.environ.get('ProgramFiles', 'C:/Program Files')) / 'Tesseract-OCR'):
            path = root / 'tesseract.exe'
            if path.is_file():
                return str(path)
    return None

def model():
    return Path(os.environ.get('SMSR_WHISPER_MODEL') or
                str(Path(os.environ['LOCALAPPDATA']) / 'SMSR/models/whisper-tiny'))

def tessdata():
    path = Path(os.environ['LOCALAPPDATA']) / 'SMSR/models/tessdata'
    return path if all((path / (lang+'.traineddata')).is_file() for lang in ['eng', 'kor']) else None
