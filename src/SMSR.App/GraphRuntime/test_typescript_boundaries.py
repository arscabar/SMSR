"""Scope, alias, overload, redaction and execution-boundary checks."""
import json
import os
from pathlib import Path
import tempfile
from unittest.mock import patch
from typescript_bundle import run


def main():
    source = '''export {};
function pick(x:number):number;
function pick(x:string):string;
function pick(x:unknown):unknown {return x;}
const a=pick(1), b=pick("PRIVATE_LITERAL_887");
const value=1, short={value};
function scope(){const value=2; return value;}
const arr=[1,2]; function rest(...values:number[]){return values;}
rest(...arr);
const secret={"PRIVATE_PROPERTY_887":"PRIVATE_VALUE_887"};
const z=secret["PRIVATE_PROPERTY_887"];
'''
    result = run({'files': [{'path': 'scope.ts', 'text': source}]})
    assert result['status'] == 'COMPILER_BINDINGS'
    a, b, rest = result['calls']
    assert a['targetDeclarationId'] != b['targetDeclarationId']
    assert rest['arguments'][0]['parameterSymbolId'] is None
    values = [s for s in result['symbols'] if s['name'] == 'value']
    assert len(values) == 2
    refs = result['references']
    start = source.index('{value}') + 1
    shorthand = next(r for r in refs if r['start'] == start)
    assert shorthand['role'] == 'REFERENCE'
    outer = next(s for s in values if s['declarations'][0]['start'] == source.index('value=1'))
    assert shorthand['symbolId'] == outer['id']
    assert 'PRIVATE_' not in json.dumps(result)
    with tempfile.TemporaryDirectory() as folder:
        sentinel = Path(folder) / 'must-not-exist'
        # Poison preloads must not reach Node. Target source must not execute.
        with patch.dict(os.environ, {'NODE_OPTIONS': '--require DOES_NOT_EXIST',
                                    'NODE_PATH': folder, 'NODE_V8_COVERAGE': folder}):
            r = run({'files': [{'path': 'noexec.js', 'text':
                'require("node:fs").writeFileSync(' + json.dumps(str(sentinel)) + ',"bad");'}]})
        assert r['status'] == 'COMPILER_CANDIDATE'
        assert not sentinel.exists() and not list(Path(folder).iterdir())
    print('TypeScript scope/overload/redaction/no-execution boundaries passed')


if __name__ == '__main__':
    main()
