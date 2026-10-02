"""Real PDF text/scan/encryption/decompression trust boundaries."""
import hashlib
import io
import tempfile
import zlib
from pathlib import Path
from pypdf import PdfWriter
from pypdf.generic import DecodedStreamObject, EncodedStreamObject, NameObject, DictionaryObject
from document_input import run

with tempfile.TemporaryDirectory() as folder:
    def extract(name, raw):
        path=Path(folder)/name;path.write_bytes(raw)
        return run(dict(root=folder,path=str(path),hash=hashlib.sha256(raw).hexdigest().upper()))
    writer=PdfWriter();writer.add_blank_page(100,100)
    output=io.BytesIO();writer.write(output)
    assert extract('scan.pdf',output.getvalue())['status']=='NO_TEXT_OR_SCAN'
    writer=PdfWriter();page=writer.add_blank_page(100,100)
    font=DictionaryObject({NameObject('/Type'):NameObject('/Font'),NameObject('/Subtype'):NameObject('/Type1'),NameObject('/BaseFont'):NameObject('/Helvetica')})
    page[NameObject('/Resources')]=DictionaryObject({NameObject('/Font'):DictionaryObject({NameObject('/F1'):writer._add_object(font)})})
    stream=DecodedStreamObject();stream.set_data(b'BT /F1 12 Tf 10 30 Td (Text evidence) Tj ET')
    page[NameObject('/Contents')]=writer._add_object(stream)
    output=io.BytesIO();writer.write(output)
    assert 'Text evidence' in extract('text.pdf',output.getvalue())['blocks'][0]['text']
    writer.encrypt('fixture-only');output=io.BytesIO();writer.write(output)
    try:extract('encrypted.pdf',output.getvalue())
    except ValueError:pass
    else:raise AssertionError('Encrypted PDF accepted')
    writer=PdfWriter();page=writer.add_blank_page(100,100)
    stream=EncodedStreamObject();stream._data=zlib.compress(b'x'*8_100_000)
    stream[NameObject('/Filter')]=NameObject('/FlateDecode')
    page[NameObject('/Contents')]=writer._add_object(stream)
    output=io.BytesIO();writer.write(output)
    try:extract('bomb.pdf',output.getvalue())
    except Exception as error:assert type(error).__name__=='LimitReachedError',type(error)
    else:raise AssertionError('PDF expansion limit ignored')
