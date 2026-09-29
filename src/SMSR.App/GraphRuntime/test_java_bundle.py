"""Actual javac attribution; no target program is executed."""
import json
from pathlib import Path
from java_bundle import run

CLASSES = Path(__file__).resolve().parents[3] / 'artifacts' / 'java-analysis'


def analyze(files):
    return run({'files': [{'path': k, 'text': v} for k, v in files.items()]}, CLASSES)


def main():
    files = {
        'p/Lib.java': '''package p; public class Lib {
          public static int pick(int x) { return x; }
          public static String pick(String x) { return x; }
          public static <T> T id(T x) { return x; }
          public static int sum(int... xs) { return xs.length; }
          public int virtual(int x) { return x; }
          public Lib(int x) { }
        }''',
        'p/Entry.java': '''package p; import static p.Lib.pick;
          class Entry { String run() { int a=pick(1), b=2;
            String s=pick("RAW_SECRET_MARKER");
            Lib.sum(a,b); new Lib(a).virtual(b);
            java.util.function.Function<String,String> f=Lib::id;
            java.util.function.Supplier<String> lazy=()->Lib.id(s);
            return Lib.id(s); }
          }'''}
    r = analyze(files)
    assert r['status'] == 'BOUND_INPUT_BUNDLE', r['diagnostics']
    picks = [c for c in r['calls'] if c['signature'] and '.pick(' in c['signature']]
    assert {c['targetId'] for c in picks} == {'source:p.Lib#pick(int)', 'source:p.Lib#pick(java.lang.String)'}
    assert all(c['dispatch'] == 'DIRECT' for c in picks)
    generic = [c for c in r['calls'] if c['kind'] == 'METHOD_INVOCATION' and c['targetId'] == 'source:p.Lib#id(java.lang.Object)']
    assert all(c['expressionType'] == 'java.lang.String' and c['resolvedType'] == '(java.lang.String)java.lang.String' for c in generic)
    assert any(c['scope'] == 'DEFERRED_LAMBDA' and c['callerId'] is None for c in generic)
    assert any(c['dispatch'] == 'VIRTUAL' and c['resolution'] == 'STATIC_TARGET_ONLY' for c in r['calls'])
    assert any(c['dispatch'] == 'METHOD_REFERENCE' and c['resolution'] == 'STATIC_TARGET_ONLY' for c in r['calls'])
    assert len([s for s in r['symbols'] if s['name'] in ('a', 'b')]) == 2
    assert 'RAW_SECRET_MARKER' not in json.dumps(r)
    varargs = next(c for c in r['calls'] if c['targetId'] == 'source:p.Lib#sum(int[])')
    assert varargs['varArgsDeclared'] and [a['parameterOrdinal'] for a in varargs['arguments']] == [0, 0]
    from test_java_boundaries import verify
    verify(analyze)
    print('Java compiler bundle self-check passed')


if __name__ == '__main__':
    main()
