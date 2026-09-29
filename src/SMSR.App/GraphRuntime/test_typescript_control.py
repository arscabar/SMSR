"""Compiler CFG with independent condition/evaluation-order checks."""
import json
from typescript_bundle import run

SOURCE = '''export function early(p:boolean,a:number,b:number){if(p)return a;return b;}
export function join(p:boolean,a:number){if(p)a++;else a--;return a;}
export function select(p:boolean,q:boolean,a:number,b:number){return p&&q?a:b;}
export function coalesce(a:any,b:any){return a??b;}
export function andSet(a:any,b:any){a&&=b;return a;}
export function orSet(a:any,b:any){a||=b;return a;}
export function nullSet(a:any,b:any){a??=b;return a;}
export function counted(n:number,p:boolean){for(let i=0;i<n;i++){if(p)continue;break;}return n;}
export function once(p:boolean,a:number){do{a++;continue;}while(p);return a;}
export function endless(){while(true){}}
export function exception(a:number){try{return a;}finally{a++;}}
export async function later(a:number){return a;}
export function optional(a:any){return a?.value;}
export function destructure(a:any){let [b]=a;return b;}
export function dynamic(a:string){return eval(a);}
export function deferred(a:number){return ()=>a;}
export function secret(){throw new Error('TS_CONTROL_SECRET');}
'''


def main():
    request=dict(files=[dict(path='control.ts',text=SOURCE)])
    result=run(request)
    assert result['status']=='COMPILER_BINDINGS', result['diagnostics']
    assert result==run(request)
    assert 'TS_CONTROL_SECRET' not in json.dumps(result)
    functions={SOURCE[f['start']:].split('function ')[1].split('(')[0]: f['control']
               for f in result['functions'] if f['kind']=='FunctionDeclaration'}
    for name,reason in dict(endless='NON_EXIT_REACHABLE_REGION',exception='STATEMENT_TryStatement',
            later='SUSPENSION_PROTOCOL',optional='OPTIONAL_CHAIN',destructure='BINDING_PATTERN',dynamic='DYNAMIC_EXECUTION').items():
        c=functions[name];assert c['reason']==reason and not c['links'],(name,c)
    for name in ('early','join','select','coalesce','andSet','orSet','nullSet','counted','once','deferred','secret'):
        assert functions[name]['status']=='NORMAL_CFG_CONTROL_DEPENDENCE',(name,functions[name])
    from test_typescript_control_paths import verify
    verify(functions,SOURCE)
    js=run(dict(files=[dict(path='logic.js',text='/** @param {boolean} p @param {number} a @param {number} b */ export function choose(p,a,b){return p?a:b;}')]))
    assert js['status']=='COMPILER_BINDINGS' and js['functions'][0]['control']['links']
    from test_control_postdominance import verify as oracle
    oracle()
    print('JS/TS normal CFG, truthy/nullish/assignment/loop control checks passed')


if __name__=='__main__':main()
