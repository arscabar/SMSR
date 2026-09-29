"""Compiler storage identities, not inferred runtime values or call targets."""
import dis


def slots(code, scope, inherited):
    cells = {name: f'{scope}:cell:{name}' for name in code.co_cellvars}
    local = {name: cells.get(name, f'{scope}:local:{name}') for name in code.co_varnames}
    free = {name: inherited.get(name) for name in code.co_freevars}
    return local, cells, free


def binding(instruction, scope, module, local, cells, free):
    op, name = instruction.opname, instruction.argval
    if instruction.opcode in dis.haslocal:
        return dict(name=name, kind='LOCAL_SLOT', bindingId=local.get(name))
    if instruction.opcode in dis.hasfree:
        dynamic = op == 'LOAD_FROM_DICT_OR_DEREF'
        return dict(name=name, kind='DYNAMIC_CLASS_OR_CELL' if dynamic else 'CELL_SLOT',
                    bindingId=None if dynamic else cells.get(name, free.get(name)))
    if op in {'LOAD_GLOBAL', 'STORE_GLOBAL', 'DELETE_GLOBAL'}:
        return dict(name=name, kind='GLOBAL_LOOKUP' if op == 'LOAD_GLOBAL' else 'GLOBAL_WRITE',
                    bindingId=None, namespace=module)
    if op in {'LOAD_NAME', 'STORE_NAME', 'DELETE_NAME', 'LOAD_FROM_DICT_OR_GLOBALS'}:
        return dict(name=name, kind='NAMESPACE_LOOKUP' if op.startswith('LOAD') else 'NAMESPACE_WRITE',
                    bindingId=None, namespace=scope)
    return None


def position(instruction, lines):
    p = instruction.positions
    if None in (p.lineno, p.end_lineno, p.col_offset, p.end_col_offset) or p.lineno < 1:
        return None
    def point(line, column):
        if not 1 <= line <= len(lines) or column > len(lines[line-1]):
            return None
        prefix = lines[line-1][:column].decode('utf-8')
        return dict(line=line-1, character=len(prefix.encode('utf-16-le')) // 2)
    start, end = point(p.lineno, p.col_offset), point(p.end_lineno, p.end_col_offset)
    return dict(start=start, end=end) if start and end else None
