"""Stable paragraph/table and worksheet coordinates, without following relations."""
from pathlib import PurePosixPath

def local(tag):
    return tag.rsplit('}', 1)[-1]

def text(node):
    return ''.join(n.text or '' for n in node.iter() if local(n.tag) == 't')

def paragraphs(tree):
    body = next(n for n in tree.iter() if local(n.tag) == 'body')
    paragraph = table = 0
    for node in body:
        if local(node.tag) == 'p':
            paragraph += 1
            yield f'paragraph:{paragraph}', text(node)
        elif local(node.tag) == 'tbl':
            table += 1
            for row, tr in enumerate((n for n in node if local(n.tag) == 'tr'), 1):
                for cell, tc in enumerate((n for n in tr if local(n.tag) == 'tc'), 1):
                    yield f'table:{table}/row:{row}/cell:{cell}', text(tc)

def sheets(archive, xml):
    names = set(archive.namelist())
    if not {'xl/workbook.xml', 'xl/_rels/workbook.xml.rels'} <= names:
        return {}
    relations = {}
    for item in xml(archive, 'xl/_rels/workbook.xml.rels'):
        target = item.get('Target', '')
        if item.get('TargetMode') == 'External' or '..' in PurePosixPath(target).parts:
            continue
        path = target.lstrip('/') if target.startswith('/') else 'xl/' + target
        if path.startswith('xl/worksheets/'):
            relations[item.get('Id')] = path
    result = {}
    for item in xml(archive, 'xl/workbook.xml').iter():
        if local(item.tag) == 'sheet':
            identity = next((v for k, v in item.attrib.items() if local(k) == 'id'), None)
            if identity in relations:
                result[relations[identity]] = item.get('name', '?') + '[' + relations[identity] + ']'
    return result
