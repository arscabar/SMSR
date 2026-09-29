"""Minimal declaration/call oracles, not a claim of complete semantic coverage."""
CASES = {
    'csharp': ('cs', 'class A { static int target(int x) { return x; } static int entry() { return target(1); } }'),
    'python': ('py', 'def target(x):\n    return x\ndef entry():\n    return target(1)\n'),
    'javascript': ('js', 'function target(x) { return x; } function entry() { return target(1); }'),
    'typescript': ('ts', 'function target(x: number): number { return x; } function entry() { return target(1); }'),
    'tsx': ('tsx', 'function target(x: number) { return x; } function entry() { return <div>{target(1)}</div>; }'),
    'java': ('java', 'class A { static int target(int x) { return x; } static int entry() { return target(1); } }'),
    'go': ('go', 'package main\nfunc target(x int) int { return x }\nfunc entry() int { return target(1) }'),
    'rust': ('rs', 'fn target(x: i32) -> i32 { x } fn entry() -> i32 { target(1) }'),
    'c': ('c', 'int target(int x) { return x; } int entry() { return target(1); }'),
    'cpp': ('cpp', 'int target(int x) { return x; } int entry() { return target(1); }'),
    'ruby': ('rb', 'def target(x)\n  x\nend\ndef entry()\n  target(1)\nend'),
    'php': ('php', '<?php function target($x) { return $x; } function entry() { return target(1); }'),
    'kotlin': ('kt', 'fun target(x: Int): Int { return x }\nfun entry(): Int { return target(1) }'),
    'swift': ('swift', 'func target(_ x: Int) -> Int { return x }\nfunc entry() -> Int { return target(1) }'),
    'scala': ('scala', 'def target(x: Int): Int = x\ndef entry(): Int = target(1)'),
    'lua': ('lua', 'local function target(x) return x end\nlocal function entry() return target(1) end'),
}

if __name__ == '__main__':
    from symbols import run
    from tree_sitter_language_pack import get_parser
    for language, (extension, source) in CASES.items():
        report = run(dict(path='sample.' + extension, source=source))
        print(language, report['status'], [s['name'] for s in report['symbols']], [s['name'] for s in report['calls']])
        if not any(s['name'] == 'target' for s in report['symbols']) or not any(c['name'] == 'target' for c in report['calls']):
            print(get_parser(language).parse(source.encode()).root_node)
