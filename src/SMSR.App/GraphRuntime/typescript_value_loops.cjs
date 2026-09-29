const {copy, merge, equal} = require('./typescript_value_state.cjs');
module.exports = (ts, g, expr, statement, n, state, controls) => {
  const isFor = ts.isForStatement(n), isDo = ts.isDoStatement(n);
  if (isFor && n.initializer) {
    if (ts.isVariableDeclarationList(n.initializer)) state = statement(n.initializer, state, controls);
    else expr(n.initializer, state, controls);
  }
  const condition = isFor ? n.condition : n.expression;
  const entry = copy(state); let head = copy(state);
  for (;;) {
    g.tick(); let tested = copy(head), control = null;
    const frame = {breaks: null, continues: null};
    if (!isDo && condition) control = expr(condition, tested, controls);
    const bodyControls = control ? [...controls, control] : controls;
    let back = statement(n.statement, copy(tested), bodyControls, frame);
    back = merge(back, frame.continues, g.tick);
    if (back && isFor && n.incrementor) expr(n.incrementor, back, controls);
    let exit = condition && !isDo ? tested : null;
    if (isDo && back) {expr(condition, back, controls); exit = back;}
    exit = merge(exit, frame.breaks, g.tick);
    const next = merge(entry, back, g.tick);
    if (equal(head, next, g.tick)) return exit;
    head = next;
  }
};
