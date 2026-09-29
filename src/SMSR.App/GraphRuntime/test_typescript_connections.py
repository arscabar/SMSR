"""Call-site identity and implementation-body mapping, not runtime execution."""
from typescript_bundle import run


def main():
    source = '''export function pick(x:number):number;
export function pick(x:string):string;
export function pick(value:unknown):unknown {return value;}
export function outer(value:number){function nested(){return 99;} return value;}
export const arrow=(value:number)=>value;
export function defaults(value=3){return value;}
export async function later(value:number){return value;}
export function* stream(value:number){yield value; return value;}
export function rest(...values:number[]){return values;}
export function destruct({value}:{value:number}){return value;}
export function receiver(this:void,value:number){return value;}
export function recurse(value:number):number{return value ? recurse(value-1) : 0;}
'''
    usage = '''import * as lib from './lib';
lib.pick(1); lib.pick('private'); lib.outer(2); lib.arrow(2);
lib.defaults(); lib.defaults(undefined); lib.later(3); lib.stream(4);
lib.rest(1); lib.rest(...[1]); lib.destruct({value:1}); lib.receiver(1);
Math.abs(1);
'''
    result = run({'files': [{'path': 'lib.ts', 'text': source}, {'path': 'use.ts', 'text': usage}]})
    assert result['status'] == 'COMPILER_BINDINGS', result['diagnostics']
    calls = {c['id']: c for c in result['calls']}
    links = [c for c in result['connections'] if calls[c['callId']]['path'] == 'use.ts']
    funcs = {f['id']: f for f in result['functions']}
    a, b, outer, arrow, default, undefined, later, stream, rest, spread, destruct, receiver, native = links
    assert a['bodyId'] == b['bodyId'] and a['callId'] != b['callId']
    assert calls[a['callId']]['targetDeclarationId'] != calls[b['callId']]['targetDeclarationId']
    assert a['inputs'][0]['parameterId'] == funcs[a['bodyId']]['parameters'][0]['id']
    assert a['returns'][0]['callId'] != b['returns'][0]['callId']
    assert len(outer['returns']) == 1 and len(arrow['returns']) == 1
    assert arrow['returns'][0]['valueKind'] == 'ARROW_VALUE'
    assert default['inputs'][0]['relation'] == 'OMITTED_ARGUMENT_UNDEFINED'
    assert undefined['inputs'][0]['defaultCondition'] == 'WHEN_ARGUMENT_IS_UNDEFINED'
    for c in (later, stream, rest, spread, destruct, native):
        assert c['status'] == 'UNAVAILABLE' and not c['inputs'] and not c['returns']
    assert len(receiver['inputs']) == 1
    recursive = next(c for c in result['connections'] if c['callerFunctionId'] == c['bodyId'])
    assert recursive['status'] == 'STATIC_BODY_CANDIDATE'
    bad = run({'files': [{'path': 'bad.ts', 'text': 'function f(x:number){return x;} f("bad");'}]})
    assert bad['connections'][0]['reason'] == 'COMPILATION_ERRORS'
    assert not bad['connections'][0]['returns']
    print('TypeScript body/overload/call-site/return-boundary checks passed')


if __name__ == '__main__':
    main()
