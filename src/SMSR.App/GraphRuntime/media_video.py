import base64
from media_process import tools, extract

def read(raw, request):
    folder, path, ffmpeg, duration, info = tools(raw)
    try:
        if not any(s.get('codec_type') == 'video' for s in info.get('streams', [])):
            raise ValueError('Missing video stream')
        frames, texts = [], []
        count = min(12, max(1, int(duration / 10) + 1))
        for i in range(count):
            time = min(duration - 0.001, duration * i / count)
            image = extract(ffmpeg, path, ['-ss', str(time), '-frames:v', '1', '-vf', 'scale=1024:1024:force_original_aspect_ratio=decrease', '-f', 'image2pipe', '-vcodec', 'mjpeg', 'pipe:1'])
            if not image or len(image) > 2_000_000:
                raise ValueError('Video frame limit')
            location = f'time:{time:.3f}:{time:.3f}'
            texts.append((location, f'Video frame at {time:.3f} seconds'))
            frames.append(dict(location=location, timeSeconds=time, mime='image/jpeg', data=base64.b64encode(image).decode()))
        engine, status = 'FFmpeg', 'FRAMES_AVAILABLE'
        if any(s.get('codec_type') == 'audio' for s in info.get('streams', [])):
            from media_audio import read as audio_read
            audio = audio_read(raw, request)
            texts.extend(audio['texts'])
            engine += '+'+audio['engine']
            status = 'TEXT_AND_FRAMES' if audio['status'] == 'TEXT_AVAILABLE' else 'FRAMES_AVAILABLE'
        return dict(texts=texts, frames=frames, status=status, engine=engine, durationSeconds=duration,
                    limitation='At most 12 sampled frames, not exhaustive scene understanding; audio uses installed local transcription only.')
    finally:
        folder.cleanup()
