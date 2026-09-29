module.exports = (ts, g) => {
  const k = ts.SyntaxKind;
  const logical = new Map([[k.AmpersandAmpersandToken, 'AND'], [k.BarBarToken, 'OR'], [k.QuestionQuestionToken, 'NULLISH'],
    [k.AmpersandAmpersandEqualsToken, 'AND'], [k.BarBarEqualsToken, 'OR'], [k.QuestionQuestionEqualsToken, 'NULLISH']]);
  function test(n, yes, no, nullish = false) {
    const id = g.node(n, nullish ? 'NULLISH_TEST' : 'CONDITION');
    g.edge(id, yes, nullish ? 'NULLISH' : 'TRUE');
    g.edge(id, no, nullish ? 'NON_NULLISH' : 'FALSE');
    return id;
  }
  function condition(n, yes, no) {
    g.tick();
    if (ts.isParenthesizedExpression(n)) return condition(n.expression, yes, no);
    if (ts.isPrefixUnaryExpression(n) && n.operator === k.ExclamationToken) return condition(n.operand, no, yes);
    if (ts.isBinaryExpression(n)) {
      if (n.operatorToken.kind === k.AmpersandAmpersandToken) return condition(n.left, condition(n.right, yes, no), no);
      if (n.operatorToken.kind === k.BarBarToken) return condition(n.left, yes, condition(n.right, yes, no));
    }
    if (ts.isConditionalExpression(n)) return condition(n.condition, condition(n.whenTrue, yes, no), condition(n.whenFalse, yes, no));
    if (n.kind === k.TrueKeyword || n.kind === k.FalseKeyword) {
      const id = g.node(n, 'CONDITION'), truth = n.kind === k.TrueKeyword;
      g.edge(id, truth ? yes : no, truth ? 'TRUE' : 'FALSE'); return id;
    }
    return read(n, test(n, yes, no));
  }
  function read(n, next) {
    g.tick(); if (!n) return next;
    if (ts.isParenthesizedExpression(n) || ts.isAsExpression(n) || ts.isTypeAssertionExpression(n) ||
      ts.isNonNullExpression(n) || ts.isSatisfiesExpression(n)) return read(n.expression, next);
    if (n.flags & ts.NodeFlags.OptionalChain) g.unsupported('OPTIONAL_CHAIN');
    const done = g.step(n, 'EVALUATE', next);
    if (ts.isConditionalExpression(n)) return condition(n.condition, read(n.whenTrue, done), read(n.whenFalse, done));
    if (ts.isBinaryExpression(n) && logical.has(n.operatorToken.kind)) {
      const mode = logical.get(n.operatorToken.kind), assignment = n.operatorToken.kind >= k.FirstAssignment;
      const right = read(n.right, assignment ? g.step(n, 'ASSIGNMENT', done) : done);
      if (mode === 'NULLISH') return read(n.left, test(n.left, right, done, true));
      return mode === 'AND' ? condition(n.left, right, done) : condition(n.left, done, right);
    }
    return require('./typescript_control_operands.cjs')(ts, g, read, n, done);
  }
  return {read, condition};
};
