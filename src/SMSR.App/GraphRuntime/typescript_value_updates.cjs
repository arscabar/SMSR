const {copy, merge} = require('./typescript_value_state.cjs');
module.exports = (ts, g, expr, n, state, controls) => {
  const k = ts.SyntaxKind;
  if (ts.isPrefixUnaryExpression(n) || ts.isPostfixUnaryExpression(n)) {
    const old = expr(n.operand, state, controls), prior = g.node(n, 'UPDATE_PRIOR_VALUE');
    const next = g.node(n, 'UPDATE_NEXT_VALUE');
    g.edge(old, prior, 'OPERAND_CANDIDATE'); g.edge(prior, next, 'OPERAND_CANDIDATE');
    g.write(n.operand, n.operand, next, state, controls);
    return ts.isPostfixUnaryExpression(n) ? prior : next;
  }
  const left = expr(n.left, state, controls);
  const conditional = [k.AmpersandAmpersandEqualsToken, k.BarBarEqualsToken, k.QuestionQuestionEqualsToken].includes(n.operatorToken.kind);
  const rightState = conditional ? copy(state) : state;
  const right = expr(n.right, rightState, conditional ? [...controls, left] : controls);
  const id = g.node(n, conditional ? 'CONDITIONAL_ASSIGNMENT_VALUE' : 'COMPOUND_OPERATION');
  g.edge(left, id, 'OPERAND_CANDIDATE'); g.edge(right, id, 'OPERAND_CANDIDATE');
  g.write(n.left, n.left, conditional ? right : id, rightState, conditional ? [...controls, left] : controls);
  if (conditional) {
    const joined = merge(state, rightState, g.tick); state.clear(); for (const pair of joined) state.set(...pair);
  }
  return id;
};
