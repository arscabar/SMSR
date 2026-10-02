"""Classify explicit project dependencies; never execute or fetch them."""
import re
import xml.etree.ElementTree as ET
from collections import defaultdict

def annotate(report, root):
    grouped = defaultdict(list)
    for node in report['nodes']:
        grouped[str(node.get('source_file'))].append(node)
    for source, nodes in grouped.items():
        path = (root / source).resolve()
        if not path.is_relative_to(root.resolve()) or path.suffix.lower() not in {'.csproj', '.fsproj', '.vbproj'} or not path.is_file():
            continue
        raw = path.read_bytes()
        if len(raw) > 2_000_000 or re.search(br'<!\s*(DOCTYPE|ENTITY)\b', raw, re.I):
            continue
        try:
            project = ET.fromstring(raw)
        except ET.ParseError:
            continue
        packages = {}
        for elem in project.iter():
            if elem.tag.rsplit('}', 1)[-1] != 'PackageReference':
                continue
            name, version = elem.get('Include', ''), elem.get('Version', '')
            if not re.fullmatch(r'[\w.-]{1,200}', name):
                continue
            if version and not re.fullmatch(r'[\w.\[\](),+*-]{1,100}', version):
                continue
            packages[name + (f' ({version})' if version else '')] = name
        for node in nodes:
            if node.get('label') in packages:
                node['_smsr_kind'] = 'concept'
                node['rationale'] = '프로젝트에 선언된 NuGet 의존성; 외부 API 구현은 분석하지 않음'
