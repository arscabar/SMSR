"""Names are transient compiler metadata, never serialized in call records."""


def collect(code, items, work):
    names=();calls={}
    for item in items:
        work(1)
        if item.opname=='KW_NAMES':
            names=code.co_consts[item.arg];work(len(names))
        elif item.opname=='CALL':
            calls[item.offset]=names;names=()
    return calls


def bind(parameters, args, names, spend, work):
    work(len(parameters)+len(args))
    if any(p['kind'] in {'VAR_POSITIONAL','VAR_KEYWORD'} for p in parameters):
        return [],'VARIADIC_PARAMETERS'
    count=sum(a['keyword'] for a in args)
    if names is None or count!=len(names):return [],'KEYWORD_METADATA_UNAVAILABLE'
    positional=[p for p in parameters if p['kind'] in {'POSITIONAL_ONLY','POSITIONAL_OR_KEYWORD'}]
    named={p['name']:p for p in parameters if p['kind'] in {'POSITIONAL_OR_KEYWORD','KEYWORD_ONLY'}}
    regular=len(args)-count
    if regular>len(positional):return [],'TOO_MANY_POSITIONAL_ARGUMENTS'
    result=[];seen=set()
    for i,arg in enumerate(args):
        work(1)
        if arg['keyword']!=(i>=regular):return [],'KEYWORD_LAYOUT_MISMATCH'
        p=positional[i] if i<regular else named.get(names[i-regular])
        if p is None:return [],'UNKNOWN_OR_POSITIONAL_ONLY_KEYWORD'
        if p['index'] in seen:return [],'DUPLICATE_ARGUMENT'
        seen.add(p['index']);spend(1)
        result.append(dict(argumentValueId=arg['valueId'],parameterValueId=p['valueId'],parameterIndex=p['index']))
    if len(seen)!=len(parameters):return [],'MISSING_ARGUMENT'
    return result,None
