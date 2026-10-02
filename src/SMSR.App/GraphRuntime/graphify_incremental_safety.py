"""Fall back rather than assume context preserves cross-file canonical merges."""
import re
from pathlib import Path

def risk(root, previous, current, changed, paths):
    if any(Path(p).suffix.lower() in ('.c', '.h', '.cpp', '.hpp') for p in paths):
        return 'C/C++ 선언·구현 병합의 전체 범위 해석 필요'
    if any(Path(p).suffix.lower() not in ('.py', '.cs') for p in paths):
        return '선택 언어의 부분 해석 동등성이 아직 검증되지 않음'
    if any(Path(p).suffix.lower() not in ('.py', '.cs') for p in changed):
        return '분석 설정·별칭·프로젝트 입력 변경'
    if any(Path(p).name == '__init__.py' for p in changed):
        return 'Python 패키지 재노출/별칭 변경 가능성'
    stems = [re.sub(r'\W', '_', str(Path(p).with_suffix(''))) for p in paths]
    if len(stems) != len(set(stems)):
        return '파일 심벌 ID 정규화 충돌 가능성'
    report = previous['report']
    if any((n.get('metadata') or {}).get('is_partial') for n in report['nodes']):
        return 'C# partial 타입의 저장소 전체 병합 필요'
    for path in paths:
        if path.endswith('.cs') and re.search(r'\bpartial\s+(?:class|struct|interface|record|void)\b',
                (root / path).read_text(encoding='utf-8')):
            return 'C# partial 선언의 저장소 전체 병합 필요'
    for path in changed:
        old_keys = previous['keys'].get(path, [])
        text = (root / path).read_text(encoding='utf-8') if path in current else ''
        if path.endswith('.cs') and ('global' in old_keys and 'using' in old_keys or re.search(r'\bglobal\s+using\b', text)):
            return 'C# 전역 using 별칭 영향 범위 확정 불가'
        old_namespaces = {str((n.get('metadata') or {}).get('namespace')) for n in report['nodes']
            if n.get('source_file') == path and (n.get('metadata') or {}).get('namespace')}
        new_namespaces = set(re.findall(r'\bnamespace\s+([\w.]+)', text))
        if path.endswith('.cs') and old_namespaces and old_namespaces != new_namespaces:
            return 'C# namespace 소유·별칭 범위 변경'
    if any(n.get('type') == 'namespace' and n.get('source_file') in changed - current.keys() for n in report['nodes']):
        return '공유 namespace 정규 소유 파일 삭제'
    return None
