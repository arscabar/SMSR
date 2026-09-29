const {copy, merge} = require('./typescript_value_state.cjs');
module.exports = (ts, g, expr) => {
  function statement(n, state, controls = [], loop = null) {
    g.tick(); if (!state) return null;
    if (ts.isBlock(n)) {
      for (const child of n.statements) {state = statement(child, state, controls, loop); if (!state) break;}
      return state;
    }
    if (ts.isVariableStatement(n)) return statement(n.declarationList, state, controls, loop);
    if (ts.isVariableDeclarationList(n)) {
      if (n.flags & ts.NodeFlags.Using) g.unsupported('RESOURCE_DISPOSAL');
      for (const d of n.declarations) {
        if (d.initializer) g.write(d, d.name, expr(d.initializer, state, controls), state, controls);
        else if (n.flags & ts.NodeFlags.Let)
          g.write(d, d.name, g.node(d, 'UNDEFINED_INITIALIZATION'), state, controls);
      }
      return state;
    }
    if (ts.isExpressionStatement(n)) {expr(n.expression, state, controls); return state;}
    if (ts.isReturnStatement(n)) {
      const id = g.node(n, 'RETURN'), value = n.expression ? expr(n.expression, state, controls) : g.node(n, 'UNDEFINED_RETURN');
      g.edge(value, id, 'RETURN_VALUE'); g.control(id, controls); return null;
    }
    if (ts.isIfStatement(n)) {
      const condition = expr(n.expression, state, controls), branch = [...controls, condition];
      return merge(statement(n.thenStatement, copy(state), branch, loop),
        n.elseStatement ? statement(n.elseStatement, copy(state), branch, loop) : copy(state), g.tick);
    }
    if (ts.isWhileStatement(n) || ts.isForStatement(n) || ts.isDoStatement(n))
      return require('./typescript_value_loops.cjs')(ts, g, expr, statement, n, state, controls);
    if (ts.isSwitchStatement(n))
      return require('./typescript_value_switch.cjs')(ts, g, expr, statement, n, state, controls, loop);
    if (ts.isBreakStatement(n) || ts.isContinueStatement(n)) {
      if (!loop || n.label) g.unsupported('LABELED_OR_NONLOOP_TRANSFER');
      const key = ts.isBreakStatement(n) ? 'breaks' : 'continues';
      loop[key] = merge(loop[key], state, g.tick); return null;
    }
    if (ts.isEmptyStatement(n)) return state;
    g.unsupported('STATEMENT_' + ts.SyntaxKind[n.kind]);
  }
  return statement;
};
