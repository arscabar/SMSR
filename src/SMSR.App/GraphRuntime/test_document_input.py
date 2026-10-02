"""Small read-only conversion and trust-boundary regression."""
import hashlib
import io
from pathlib import Path
import tempfile
import zipfile
from document_input import run

def archive(parts):
    output = io.BytesIO()
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED) as file:
        for name, value in parts.items():
            file.writestr(name, value)
    return output.getvalue()

with tempfile.TemporaryDirectory() as folder:
    root = Path(folder)
    def extract(name, raw, hash=None):
        path = root / name
        path.write_bytes(raw)
        return run(dict(root=folder, path=str(path), hash=hash or hashlib.sha256(raw).hexdigest().upper()))
    doc = archive({'word/document.xml': '<document><body><p><t>Requirement</t></p><tbl><tr><tc><p><t>Reason</t></p></tc></tr></tbl><p><t>' + 'a'*600 + ' api_key=abcdefghijk</t></p></body></document>'})
    result = extract('plan.docx', doc)
    assert [b['location'] for b in result['blocks']] == ['paragraph:1', 'table:1/row:1/cell:1']
    assert result['excludedBlocks'] == 1
    book = archive({'xl/worksheets/sheet1.xml': '<worksheet><c r="A1" t="inlineStr"><is><t>Value</t></is></c><c r="A2"><f>1+2</f><v>3</v></c></worksheet>',
        'xl/workbook.xml': '<workbook xmlns:r="r"><sheet name="Requirements" r:id="id1"/></workbook>',
        'xl/_rels/workbook.xml.rels': '<Relationships><Relationship Id="id1" Target="worksheets/sheet1.xml"/></Relationships>'})
    result = extract('plan.xlsx', book)
    assert result['blocks'][0]['location'].startswith('Requirements[')
    assert result['blocks'][1]['location'].endswith(':A2:cached')
    assert extract('visible.html', b'<p>Visible</p><script>hidden</script>')['blocks'][0]['text'] == 'Visible'
    for raw, digest in [(doc, '0'*64), (archive({'../bad': 'x'}), None),
        (archive({'word/document.xml': '<!DOCTYPE bad><document/>'}), None),
        (archive({'word/vbaProject.bin': 'x'}), None)]:
        try:
            extract('bad.docx', raw, digest)
        except (ValueError, KeyError):
            pass
        else:
            raise AssertionError('Unsafe input accepted')
import test_document_pdf
print('PDF/Office coordinates, scan status, secrets, stale/active/XML/traversal rejection passed')
