import io
import importlib.util
from contextlib import closing

def read(raw, page_number):
    if importlib.util.find_spec('pypdfium2') is None:
        return []
    import pypdfium2 as pdfium
    from media_image import read as image_read
    from media_tools import tool
    if not tool('tesseract'):
        return []
    with closing(pdfium.PdfDocument(raw)) as document:
        if len(document) > 50:
            raise ValueError('Scanned PDF OCR page limit: 50')
        page = document[page_number]
        width, height = page.get_size()
        if width * height * 4 > 16_000_000:
            raise ValueError('PDF raster dimensions limit')
        bitmap = page.render(scale=2)
        image = bitmap.to_pil()
        buffer = io.BytesIO()
        image.save(buffer, format='PNG')
        result = image_read(buffer.getvalue(), {})
        bitmap.close()
        page.close()
    return [(f'page:{page_number+1}:{location}', text) for location, text in result['texts']]
