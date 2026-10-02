"""PDF text, with bounded local OCR for scanned pages when explicitly installed."""
import io
from pypdf import PdfReader
import pypdf.filters
pypdf.filters.ZLIB_MAX_OUTPUT_LENGTH = 8_000_000

def read(raw, _):
    reader = PdfReader(io.BytesIO(raw), strict=True)
    if reader.is_encrypted or len(reader.pages) > 500:
        raise ValueError('Encrypted/page-limited PDF')
    for number, page in enumerate(reader.pages, 1):
        content = page.get_contents()
        if content is not None and len(content.get_data()) > 8_000_000:
            raise ValueError('PDF content expansion limit')
        text = page.extract_text() or ''
        if text.strip():
            yield f'page:{number}', text
        else:
            from document_scan_ocr import read as ocr
            yield from ocr(raw, number-1)
