module.exports = (ts, facts, charge, tick, fn, invalid) => {
  const nodes = [], transfers = [];
  function unsupported(reason) {const error = Error('unsupported control model'); error.reason = reason; throw error;}
  function node(ast, kind) {charge(); const id = nodes.length; nodes.push({id, kind, source: facts.location(ast)}); return id;}
  function edge(source, target, outcome = 'ALWAYS') {charge(); transfers.push({source, target, outcome});}
  function step(ast, kind, next) {const id = node(ast, kind); edge(id, next); return id;}
  const g = {node, edge, step, unsupported, tick};
  try {
    if (invalid) unsupported('COMPILATION_ERRORS');
    if (fn.asteriskToken || fn.modifiers?.some(m => m.kind === ts.SyntaxKind.AsyncKeyword)) unsupported('SUSPENSION_PROTOCOL');
    if (ts.isConstructorDeclaration(fn)) unsupported('CONSTRUCTOR_PROTOCOL');
    if (fn.parameters.some(p => p.initializer || p.dotDotDotToken || !ts.isIdentifier(p.name))) unsupported('PARAMETER_PROTOCOL');
    const expr = require('./typescript_control_expressions.cjs')(ts, g);
    const statement = require('./typescript_control_statements.cjs')(ts, g, expr);
    const body = ts.isBlock(fn.body) ? statement(fn.body, -1) : expr.read(fn.body, step(fn.body, 'RETURN', -1));
    return {status: 'NORMAL_CFG', entry: step(fn.body, 'ENTRY', body), exitNode: -1, nodes, transfers};
  } catch (error) {
    if (!error.reason) throw error;
    return {status: 'UNAVAILABLE', reason: error.reason, nodes: [], transfers: []};
  }
};
