module.exports = (ts, g, expr, n, state, controls) => {
  if (!ts.isIdentifier(n.expression) || n.expression.text === 'eval' || n.questionDotToken)
    g.unsupported('DYNAMIC_OR_OPTIONAL_CALL');
  if (n.arguments.some(ts.isSpreadElement)) g.unsupported('SPREAD_ARGUMENTS');
  const target = expr(n.expression, state, controls), site = g.node(n, 'CALL_INPUT_BOUNDARY');
  g.edge(target, site, 'CALL_TARGET_CANDIDATE');
  n.arguments.forEach((arg, index) => {
    const value = expr(arg, state, controls), id = g.node(arg, 'CALL_ARGUMENT_' + index, {index});
    g.edge(value, id, 'ARGUMENT_VALUE'); g.edge(id, site, 'CALL_ARGUMENT');
  });
  g.control(site, controls);
  // No input-to-result edge: a selected signature does not summarize the body.
  return g.node(n, 'OPAQUE_CALL_RESULT');
};
