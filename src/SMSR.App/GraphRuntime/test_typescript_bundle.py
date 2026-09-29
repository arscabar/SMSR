"""Installed compiler oracle; analyzed source is never executed."""
import json
from typescript_bundle import run


def main():
    files = [
        {'path': 'math.ts', 'text': 'export function identity<T>(value:T):T { return value; }'},
        {'path': 'use.ts', 'text': 'import {identity as select} from "./math"; const n=select(42); const s=select("SECRET_LITERAL_991");'},
        {'path': 'use.js', 'text': '/** @param {number} value */ function double(value) { return value*2; } double(3);'},
        {'path': 'view.tsx', 'text': 'declare namespace JSX { interface Element {} } function View(props:{value:number}):JSX.Element {return {};} const v=<View value={1}/>;'}]
    result = run({'files': files})
    assert result['status'] == 'COMPILER_BINDINGS', result['diagnostics']
    calls = result['calls']
    assert len(calls) == 4
    assert [c['returnType']['kind'] for c in calls[:2]] == ['NUMBER', 'STRING']
    assert calls[0]['targetDeclarationId'] == calls[1]['targetDeclarationId']
    assert calls[0]['target']['path'] == 'math.ts'
    assert calls[2]['parameters'][0]['type']['kind'] == 'NUMBER'
    assert calls[3]['mapping'] == 'SPREAD_REST_OR_JSX_NOT_EXPANDED'
    assert calls[0]['arguments'][0]['parameterSymbolId'] in {s['id'] for s in result['symbols']}
    assert 'SECRET_LITERAL_991' not in json.dumps(result)
    errors = run({'files': [{'path': 'bad.ts', 'text': 'import {x} from "outside"; const n:number="SECRET_ERROR_991";'}]})
    assert errors['status'] == 'COMPILER_CANDIDATE'
    assert 'SECRET_ERROR_991' not in json.dumps(errors)
    for path in ('../escape.ts', '/abs.ts', 'C:/abs.ts', 'a\\b.ts'):
        try:
            run({'files': [{'path': path, 'text': ''}]})
            raise AssertionError('unsafe path accepted')
        except ValueError:
            pass
    print('TypeScript/JavaScript/TSX compiler oracle passed')


if __name__ == '__main__':
    main()
