"""Conservative lexical may-depend graph. Never claims whole-program safety."""
from symbols import walk, FUNCTIONS, name
from taint import findings

CONTROL = {'if_statement','for_statement','while_statement','foreach_statement','for_in_statement','switch_statement','if_expression','match_expression','try_statement'}
ASSIGN = {'assignment','assignment_expression','augmented_assignment','variable_declarator','init_declarator','short_var_declaration','let_declaration'}
IDENT = {'identifier','field_identifier','property_identifier','simple_identifier'}

def identifiers(node):
    return {n.text.decode('utf-8')[:128] for n in walk(node) if n.type in IDENT} if node else set()

def analyze(functions, path):
    statements, edges = [], []
    for function in functions[:200]:
        records = []
        def visit(node, control=None):
            if node != function and node.type in FUNCTIONS:
                return
            is_statement = node.type.endswith('_statement') or node.type in ASSIGN or node.type in CONTROL or node.type == 'call'
            if is_statement:
                sid = f'{path}:{node.start_byte}:{node.type}'
                lhs = node.child_by_field_name('left') or node.child_by_field_name('name') or node.child_by_field_name('pattern')
                defs = identifiers(lhs) if node.type in ASSIGN else set()
                condition = node.child_by_field_name('condition')
                uses = identifiers(condition if node.type in CONTROL else node) - defs
                record = dict(id=sid, line=node.start_point.row+1, kind=node.type, defines=sorted(defs), uses=sorted(uses), scope=function.start_byte)
                records.append(record)
                if control:
                    edges.append(dict(source=control, target=sid, relation='CONTROL_MAY_DEPEND'))
                if node.type in CONTROL:
                    control = sid
            for child in node.named_children:
                visit(child, control)
        visit(function)
        if len(records) + len(statements) > 2000:
            raise ValueError('Flow exceeds 2000 statements; split the file')
        definitions = {}
        for record in records:
            for variable in record['defines']:
                definitions.setdefault(variable, []).append(record['id'])
        # ponytail: flow-insensitive union handles branches/loops conservatively; SSA required for precise kills/aliasing.
        for record in records:
            for variable in record['uses']:
                for source in definitions.get(variable, []):
                    if source != record['id']:
                        edges.append(dict(source=source, target=record['id'], relation='DATA_MAY_DEPEND', variable=variable))
                        if len(edges) > 10000:
                            raise ValueError('Flow exceeds 10000 edges; split the file')
        statements.extend(records)
    return dict(statements=statements, edges=edges, findings=findings(statements, edges),
                flowTruncated=len(functions)>200, flowCoverage='Function-local lexical MAY_DEPEND; nested scopes, aliases, sanitizers and interprocedural flow are not resolved')
