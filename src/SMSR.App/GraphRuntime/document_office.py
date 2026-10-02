"""ZIP/XML Office input with coordinates, never extraction to disk or evaluation."""
import io
import zipfile
import xml.etree.ElementTree as ET
from pathlib import PurePosixPath

def local(tag):
    return tag.rsplit('}', 1)[-1]

def xml(archive, name):
    info = archive.getinfo(name)
    if info.file_size > 8_000_000:
        raise ValueError('XML size limit')
    raw = archive.read(info)
    if b'<!DOCTYPE' in raw.upper() or b'<!ENTITY' in raw.upper() or b'\0' in raw:
        raise ValueError('DTD/entity/encoding forbidden')
    return ET.fromstring(raw)

def read(raw, extension):
    with zipfile.ZipFile(io.BytesIO(raw)) as archive:
        items = archive.infolist()
        if len(items) > 2000 or len({i.filename for i in items}) != len(items):
            raise ValueError('Archive entry limit/duplicate')
        if any(i.filename.startswith('/') or '\\' in i.filename or ':' in i.filename or
               '..' in PurePosixPath(i.filename).parts or (i.external_attr >> 16) & 0o170000 == 0o120000 for i in items):
            raise ValueError('Archive linked/traversal entry')
        if sum(i.file_size for i in items) > 32_000_000 or any(i.flag_bits & 1 or
                i.file_size > max(1024, i.compress_size) * 100 for i in items):
            raise ValueError('Encrypted/archive expansion limit')
        if any('vbaProject' in i.filename or 'externalLinks/' in i.filename for i in items):
            raise ValueError('Active Office content excluded')
        if extension == '.docx':
            tree = xml(archive, 'word/document.xml')
            from document_coordinates import paragraphs
            yield from paragraphs(tree)
            return
        names = {i.filename for i in items}
        strings = []
        if 'xl/sharedStrings.xml' in names:
            strings = [''.join(t.text or '' for t in n.iter() if local(t.tag) == 't')
                       for n in xml(archive, 'xl/sharedStrings.xml') if local(n.tag) == 'si']
        from document_coordinates import sheets
        sheet_names = sheets(archive, xml)
        for name in sorted(n for n in names if n.startswith('xl/worksheets/sheet') and n.endswith('.xml')):
            for cell in xml(archive, name).iter():
                if local(cell.tag) != 'c':
                    continue
                value = next((n.text or '' for n in cell if local(n.tag) == 'v'), '')
                if cell.get('t') == 's':
                    value = strings[int(value)]
                elif cell.get('t') == 'inlineStr':
                    value = ''.join(n.text or '' for n in cell.iter() if local(n.tag) == 't')
                # Formula text is not evaluated; cached values are distinctly labelled.
                suffix = ':cached' if any(local(n.tag) == 'f' for n in cell) else ''
                yield sheet_names.get(name, name) + ':' + cell.get('r', '?') + suffix, value
