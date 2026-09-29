"""Compiler parameter slots and call sites, never keyword/default values."""
import inspect


def parameters(code, function, spend):
    kinds=['POSITIONAL_ONLY']*code.co_posonlyargcount
    kinds+=['POSITIONAL_OR_KEYWORD']*(code.co_argcount-code.co_posonlyargcount)
    kinds+=['KEYWORD_ONLY']*code.co_kwonlyargcount
    if code.co_flags & inspect.CO_VARARGS:kinds.append('VAR_POSITIONAL')
    if code.co_flags & inspect.CO_VARKEYWORDS:kinds.append('VAR_KEYWORD')
    result=[]
    for index,kind in enumerate(kinds):
        spend(1)
        name=code.co_varnames[index];binding=function['locals'][name]
        result.append(dict(index=index,name=name,kind=kind,bindingId=binding,valueId=binding+':entry'))
    return result


def calls(nodes, describe, spend, work):
    sites={}
    for n in nodes:
        work(1)
        if n['kind'] not in {'CALL_TARGET','CALL_ARGUMENT','CALL_RECEIVER_CANDIDATE','OPAQUE_CALL_RESULT'}:continue
        offset=n['offset']
        if offset not in sites:
            spend(1)
            sites[offset]=dict(offset=offset,source=n['source'],target=None,receiver=None,arguments=[],resultValueId=None)
        site=sites[offset]
        if n['kind']=='OPAQUE_CALL_RESULT':site['resultValueId']=n['id']
        elif n['kind']=='CALL_ARGUMENT':
            site['arguments'].append(dict(index=n['argument'],keyword=n['keyword'],**describe(n)))
        elif n['kind']=='CALL_TARGET':site['target']=describe(n)
        else:site['receiver']=describe(n)
    for site in sites.values():site['arguments'].sort(key=lambda n:n['index'])
    return [sites[k] for k in sorted(sites)]
