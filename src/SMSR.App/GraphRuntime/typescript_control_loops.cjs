module.exports = (ts, g, expr, statement, n, next) => {
  const isFor = ts.isForStatement(n), isDo = ts.isDoStatement(n);
  const gate = g.node(n, 'LOOP_GATE');
  const resume = isFor ? expr.read(n.incrementor, gate) : gate;
  const body = statement(n.statement, resume, {exit: next, resume});
  const condition = isFor ? n.condition : n.expression;
  g.edge(gate, condition ? expr.condition(condition, body, next) : body);
  if (isDo) return body;
  if (isFor && n.initializer) return ts.isVariableDeclarationList(n.initializer) ?
    statement(n.initializer, gate) : expr.read(n.initializer, gate);
  return gate;
};
