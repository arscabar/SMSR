module.exports = (ts, g, expr, statement, n, next, outer) => {
  const clauses = n.caseBlock.clauses, entries = [];
  const transfer = {exit: next, resume: outer?.resume};
  let body = next, fallback = next;
  // Bodies fall through in source order, never through the next case test.
  for (let i = clauses.length - 1; i >= 0; i--) {
    g.tick();
    const clause = clauses[i];
    for (let j = clause.statements.length - 1; j >= 0; j--)
      body = statement(clause.statements[j], body, transfer);
    entries[i] = body;
    if (ts.isDefaultClause(clause)) fallback = body;
  }
  // Default is chosen only after ALL selectors fail, even when in the middle.
  let search = fallback;
  for (let i = clauses.length - 1; i >= 0; i--) {
    g.tick();
    const clause = clauses[i];
    if (ts.isDefaultClause(clause)) continue;
    const test = g.node(clause.expression, 'CASE_TEST');
    g.edge(test, entries[i], 'CASE_MATCH');
    g.edge(test, search, 'CASE_NO_MATCH');
    search = expr.read(clause.expression, test);
  }
  // Strict comparison uses this captured value, not a reevaluated expression.
  return expr.read(n.expression, g.step(n.expression, 'SWITCH_VALUE', search));
};
