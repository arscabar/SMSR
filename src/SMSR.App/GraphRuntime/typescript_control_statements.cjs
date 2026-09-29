module.exports = (ts, g, expr) => {
  function statement(n, next, loop = null) {
    g.tick();
    if (ts.isBlock(n)) {
      for (let i = n.statements.length - 1; i >= 0; i--) next = statement(n.statements[i], next, loop);
      return next;
    }
    if (ts.isEmptyStatement(n) || ts.isFunctionDeclaration(n)) return next;
    if (ts.isVariableStatement(n)) return statement(n.declarationList, next, loop);
    if (ts.isVariableDeclarationList(n)) {
      if (n.flags & ts.NodeFlags.Using) g.unsupported('RESOURCE_DISPOSAL');
      for (let i = n.declarations.length - 1; i >= 0; i--) {
        const d = n.declarations[i];
        if (!ts.isIdentifier(d.name)) g.unsupported('BINDING_PATTERN');
        next = expr.read(d.initializer, g.step(d, 'DECLARE', next));
      }
      return next;
    }
    if (ts.isExpressionStatement(n)) return expr.read(n.expression, next);
    if (ts.isReturnStatement(n) || ts.isThrowStatement(n))
      return expr.read(n.expression, g.step(n, ts.isReturnStatement(n) ? 'RETURN' : 'EXPLICIT_THROW', -1));
    if (ts.isIfStatement(n)) return expr.condition(n.expression, statement(n.thenStatement, next, loop),
      n.elseStatement ? statement(n.elseStatement, next, loop) : next);
    if (ts.isWhileStatement(n) || ts.isDoStatement(n) || ts.isForStatement(n))
      return require('./typescript_control_loops.cjs')(ts, g, expr, statement, n, next);
    if (ts.isSwitchStatement(n))
      return require('./typescript_control_switch.cjs')(ts, g, expr, statement, n, next, loop);
    if (ts.isBreakStatement(n) || ts.isContinueStatement(n)) {
      const isBreak = ts.isBreakStatement(n);
      if (!loop || n.label || (!isBreak && loop.resume == null)) g.unsupported('LABELED_OR_NONLOOP_TRANSFER');
      return g.step(n, isBreak ? 'BREAK' : 'CONTINUE', isBreak ? loop.exit : loop.resume);
    }
    g.unsupported('STATEMENT_' + ts.SyntaxKind[n.kind]);
  }
  return statement;
};
