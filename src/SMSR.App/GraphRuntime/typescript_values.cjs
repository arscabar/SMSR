module.exports = (ts, checker, facts, charge, tick, fn, invalid) => {
  const g = require('./typescript_value_graph.cjs')(ts, checker, facts, charge, tick);
  try {
    if (invalid) g.unsupported('COMPILATION_ERRORS');
    if (fn.asteriskToken || fn.modifiers?.some(m => m.kind === ts.SyntaxKind.AsyncKeyword)) g.unsupported('SUSPENSION_PROTOCOL');
    if (ts.isConstructorDeclaration(fn)) g.unsupported('CONSTRUCTOR_PROTOCOL');
    const state = new Map();
    function seed(name, ast, kind) {
      if (!ts.isIdentifier(name)) g.unsupported('BINDING_PATTERN');
      const symbolId = g.symbol(name); g.locals.add(symbolId);
      if (!state.has(symbolId)) state.set(symbolId, new Set([g.node(ast, kind, {symbolId})]));
    }
    for (const p of fn.parameters) {
      if (ts.isIdentifier(p.name) && p.name.text === 'this') continue;
      if (p.initializer || p.dotDotDotToken) g.unsupported('DEFAULT_OR_REST_PARAMETER');
      seed(p.name, p, 'PARAMETER_INPUT');
    }
    function collect(n) {
      tick(); if (ts.isFunctionLike(n) || ts.isClassLike(n)) g.unsupported('NESTED_FUNCTION_OR_CLASS');
      if (ts.isVariableDeclaration(n)) seed(n.name, n,
        n.parent.flags & ts.NodeFlags.BlockScoped ? 'UNINITIALIZED_LOCAL_CANDIDATE' : 'HOISTED_UNDEFINED');
      ts.forEachChild(n, collect);
    }
    collect(fn.body);
    const expr = require('./typescript_value_expressions.cjs')(ts, g);
    if (ts.isBlock(fn.body)) {
      const after = require('./typescript_value_statements.cjs')(ts, g, expr)(fn.body, state);
      if (after) g.node(fn.body, 'IMPLICIT_UNDEFINED_RETURN_CANDIDATE');
    } else {
      const value = expr(fn.body, state), result = g.node(fn.body, 'RETURN'); g.edge(value, result, 'RETURN_VALUE');
    }
    return {status: 'VALUE_FLOW_CANDIDATES', nodes: [...g.nodes.values()], edges: [...g.edges.values()],
      boundary: 'Normal-flow may-depend; lexical controls only. No feasible-path, exception, heap/alias, operator effects or interprocedural summary guarantee.'};
  } catch (error) {
    if (!error.reason) throw error;
    return {status: 'UNAVAILABLE', reason: error.reason, nodes: [], edges: []};
  }
};
