"""Capture parser recovery also for languages that upstream never caches."""
from contextlib import contextmanager
from graphify_normalize import line_of
from graphify_csharp_recovery import recover
from graphify_cpp_recovery import recover as recover_cpp

@contextmanager
def collect(module, root):
    results = {}
    original = module._safe_extract_with_xaml_root
    def capture(extractor, path, anchor):
        value = recover_cpp(module, path, recover(module, path, original(extractor, path, anchor)))
        results[path] = value
        return value
    module._safe_extract_with_xaml_root = capture
    try:
        yield results
    finally:
        module._safe_extract_with_xaml_root = original

def issues(paths, results, module, root):
    for path in paths:
        value = results.get(path) or module.load_cached(path, root, cache_root=root) or {}
        for line in value.get('smsr_recovery_lines', []):
            yield dict(ownerPath=path.relative_to(root).as_posix(), sourceLine=line,
                relation='PREPROCESSOR_RECOVERY', reason='조건부 특성 전용 블록의 지시문을 분석 복사본에서 정규화해 타입·메서드 소유 관계 복원. 원본·줄 위치 유지; 실행 분기 선택 없음', candidatePaths=[])
        for line, kind in value.get('smsr_syntax_recovery', []):
            yield dict(ownerPath=path.relative_to(root).as_posix(), sourceLine=line,
                relation='SYNTAX_RECOVERY', reason=f'문법 호환 복구: {kind}. 원문 불변·줄 위치 유지; 실행 순서 판정 아님', candidatePaths=[])
        error = value.get('parse_errors')
        if error:
            count = max(0, len(value.get('nodes', [])) - 1)
            scope = '여러 줄 복구 영역' if error.get('multiline_error') else '구문 복구 지점'
            yield dict(ownerPath=path.relative_to(root).as_posix(),
                sourceLine=line_of(error.get('first_error_line')), relation='PARSE_ERROR',
                reason=f'Graphify {scope}; 심벌 {count}개 추출. 문법 지원 차이/소스 오류를 원문에서 확인; 해당 영역 누락 가능',
                candidatePaths=[])
