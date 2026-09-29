const {copy, merge} = require('./typescript_value_state.cjs');
module.exports = (ts, g) => {
  function expr(n, state, controls = []) {
    g.tick(); const k = ts.SyntaxKind;
    if (ts.isParenthesizedExpression(n) || ts.isAsExpression(n) || ts.isTypeAssertionExpression(n) || ts.isNonNullExpression(n))
      return expr(n.expression, state, controls);
    if (ts.isIdentifier(n)) {
      if (n.text === 'arguments') g.unsupported('ARGUMENTS_ALIAS');
      const symbolId = g.symbol(n), local = g.locals.has(symbolId);
      const id = g.node(n, local ? 'LOCAL_READ' : 'EXTERNAL_READ', {symbolId});
      for (const source of state.get(symbolId) || []) g.edge(source, id, 'REACHING_DEFINITION');
      return id;
    }
    if (ts.isLiteralExpression(n) || [k.TrueKeyword, k.FalseKeyword, k.NullKeyword].includes(n.kind))
      return g.node(n, 'CONSTANT_VALUE_REDACTED');
    if (ts.isCallExpression(n)) return require('./typescript_value_calls.cjs')(ts, g, expr, n, state, controls);
    if (ts.isBinaryExpression(n)) {
      const op = n.operatorToken.kind;
      if (op === k.EqualsToken) return g.write(n.left, n.left, expr(n.right, state, controls), state, controls);
      if (op >= k.FirstAssignment && op <= k.LastAssignment)
        return require('./typescript_value_updates.cjs')(ts, g, expr, n, state, controls);
      const left = expr(n.left, state, controls);
      if (op === k.CommaToken) return expr(n.right, state, controls);
      const branch = [k.AmpersandAmpersandToken, k.BarBarToken, k.QuestionQuestionToken].includes(op);
      const alternate = branch ? copy(state) : state;
      const right = expr(n.right, alternate, branch ? [...controls, left] : controls);
      if (branch) {const joined = merge(state, alternate, g.tick); state.clear(); for (const pair of joined) state.set(...pair);}
      const id = g.node(n, branch ? 'SHORT_CIRCUIT_VALUE_CANDIDATE' : 'OPERATION');
      g.edge(left, id, 'OPERAND_CANDIDATE'); g.edge(right, id, 'OPERAND_CANDIDATE'); return id;
    }
    if (ts.isPostfixUnaryExpression(n) || ts.isPrefixUnaryExpression(n) && [k.PlusPlusToken, k.MinusMinusToken].includes(n.operator))
      return require('./typescript_value_updates.cjs')(ts, g, expr, n, state, controls);
    if (ts.isPrefixUnaryExpression(n)) {
      const value = expr(n.operand, state, controls), id = g.node(n, 'OPERATION');
      g.edge(value, id, 'OPERAND_CANDIDATE'); return id;
    }
    g.unsupported('EXPRESSION_' + ts.SyntaxKind[n.kind]);
  }
  return expr;
};
