import importlib.util
import os
from pathlib import Path
from media_process import tools, extract
from media_tools import model as configured_model

def read(raw, request):
    folder, source, ffmpeg, duration, _ = tools(raw)
    try:
        texts = [('time:0:%.3f' % duration, 'Audio duration %.3f seconds' % duration)]
        model_path = configured_model()
        if not model_path or not Path(model_path).is_dir() or importlib.util.find_spec('faster_whisper') is None:
            return dict(texts=texts, frames=[], status='TRANSCRIPTION_UNAVAILABLE', engine='FFmpeg', durationSeconds=duration)
        from faster_whisper import WhisperModel
        audio = Path(folder.name) / 'audio.wav'
        audio.write_bytes(extract(ffmpeg, source, ['-vn', '-ac', '1', '-ar', '16000', '-f', 'wav', 'pipe:1']))
        model = WhisperModel(str(model_path), device='cpu', compute_type='int8', cpu_threads=4, local_files_only=True)
        segments, _ = model.transcribe(str(audio), vad_filter=True, beam_size=1)
        for segment in segments:
            if not 0 <= segment.start <= segment.end <= duration + 0.1:
                raise ValueError('Transcript time bounds')
            texts.append((f'time:{min(segment.start,duration):.3f}:{min(segment.end,duration):.3f}', segment.text))
            if len(texts) > 2000:
                raise ValueError('Transcript segment limit')
        return dict(texts=texts, frames=[], status='TEXT_AVAILABLE' if len(texts)>1 else 'NO_SPEECH',
                    engine='faster-whisper:local', durationSeconds=duration)
    finally:
        folder.cleanup()
