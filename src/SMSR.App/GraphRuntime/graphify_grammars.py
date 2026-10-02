"""Adapt Graphify grammar imports to SMSR's already-installed language pack."""
import sys
from types import ModuleType
from tree_sitter_language_pack import get_binding

def install():
    names = dict(c_sharp='csharp', python='python', javascript='javascript',
        typescript='typescript', go='go', rust='rust', java='java', c='c', cpp='cpp',
        ruby='ruby', php='php', kotlin='kotlin', swift='swift', scala='scala', lua='lua',
        groovy='groovy', zig='zig', powershell='powershell', elixir='elixir',
        objc='objc', julia='julia', verilog='verilog', fortran='fortran', bash='bash',
        json='json', sql='sql', hcl='hcl', pascal='pascal', ocaml='ocaml',
        commonlisp='commonlisp', solidity='solidity')
    for name, language in names.items():
        # The pack's Groovy tree is incompatible with upstream's Java-shaped grammar.
        if name in {'groovy', 'zig'}:
            continue
        module = ModuleType('tree_sitter_' + name)
        module.language = lambda selected=language: get_binding(selected)
        if name == 'typescript':
            module.language_typescript = lambda: get_binding('typescript')
            module.language_tsx = lambda: get_binding('tsx')
        if name == 'php':
            module.language_php = lambda: get_binding('php')
        if name == 'ocaml':
            module.language_ocaml = lambda: get_binding('ocaml')
            module.language_ocaml_interface = lambda: get_binding('ocaml_interface')
        sys.modules[module.__name__] = module
