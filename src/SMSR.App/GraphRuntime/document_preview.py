"""Read-only page raster. No PDF scripts, attachments or network retrieval."""
import base64
import hashlib
import io
from contextlib import closing
from document_input import validated

def run(request):
    import pypdfium2 as pdfium
    path, raw, digest = validated(request)
    page_number = request['page']
    if path.suffix.lower() != '.pdf' or type(page_number) is not int or not 1 <= page_number <= 50:
        raise ValueError('PDF page preview limit')
    with closing(pdfium.PdfDocument(raw)) as document:
        if page_number > len(document):
            raise ValueError('Missing PDF page')
        page = document[page_number-1]
        width, height = page.get_size()
        if min(width,height) <= 0 or width * height * 4 > 16_000_000:
            raise ValueError('PDF raster dimensions limit')
        bitmap = page.render(scale=2)
        image = bitmap.to_pil()
        buffer = io.BytesIO()
        image.convert('RGB').save(buffer,format='JPEG',quality=85)
        size = image.size
        bitmap.close()
        page.close()
    if len(buffer.getvalue()) > 8_000_000 or hashlib.sha256(path.read_bytes()).hexdigest().upper() != digest:
        raise ValueError('PDF preview changed/output limit')
    return dict(sourceHash=digest,page=page_number,width=size[0],height=size[1],data=base64.b64encode(buffer.getvalue()).decode())
