import json
import os
from pathlib import Path
import tempfile


def verify(analyze):
    bad = analyze({'Bad.java': 'class Bad { int f() { return missing(); } }'})
    assert bad['status'] == 'COMPILATION_ERRORS' and bad['diagnosticCount'] > 0
    assert all(c['resolution'] in ('UNRESOLVED', 'COMPILER_CANDIDATE') for c in bad['calls'])
    source = 'class 한글 {\r\n\t// 😀\r\n\tstatic int f(int x) { return x; }\r\n\tint g() { return f(1); }\r\n}'
    r = analyze({'한글.java': source})
    call = next(c for c in r['calls'] if c['targetId'] == 'source:한글#f(int)')
    assert call['source']['start'] == {'line': 3, 'character': 18}, call
    secret = analyze({'A.java': '''import java.lang.annotation.*;
      @Target(ElementType.TYPE_USE) @interface Secret { String value(); }
      class A { static @Secret("ANNOTATION_SECRET_MARKER") String f() { return "x"; } }
      '''})
    assert 'ANNOTATION_SECRET_MARKER' not in json.dumps(secret)
    for path in ('../A.java', '/A.java', 'C:/A.java', 'a\\A.java', 'a//A.java'):
        try:
            analyze({path: 'class A {}'})
            raise AssertionError('Unsafe Java path accepted')
        except ValueError:
            pass
    with tempfile.TemporaryDirectory(prefix='smsr-java-check-') as directory:
        previous = os.getcwd()
        try:
            os.chdir(directory)
            Path('Outside.java').write_text('class Outside {}', encoding='utf-8')
            result = analyze({'A.java': 'class A { Outside value; }'})
            assert result['status'] == 'COMPILATION_ERRORS', 'Implicit source discovery escaped input bundle'
            assert sorted(p.name for p in Path('.').iterdir()) == ['Outside.java'], 'Compiler emitted files'
        finally:
            os.chdir(previous)
