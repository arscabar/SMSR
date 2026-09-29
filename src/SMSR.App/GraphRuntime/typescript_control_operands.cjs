module.exports = (ts, g, read, n, next) => {
  const k = ts.SyntaxKind; let children = [];
  if (ts.isIdentifier(n) || ts.isLiteralExpression(n) || ts.isFunctionExpression(n) || ts.isArrowFunction(n) ||
    [k.TrueKeyword, k.FalseKeyword, k.NullKeyword, k.ThisKeyword, k.SuperKeyword].includes(n.kind)) return next;
  if (ts.isBinaryExpression(n)) {
    if (n.operatorToken.kind === k.EqualsToken && !ts.isIdentifier(n.left) && !ts.isPropertyAccessExpression(n.left) && !ts.isElementAccessExpression(n.left))
      g.unsupported('DESTRUCTURING_ASSIGNMENT');
    children = [n.left, n.right];
  } else if (ts.isPrefixUnaryExpression(n) || ts.isPostfixUnaryExpression(n)) children = [n.operand];
  else if (ts.isCallExpression(n) || ts.isNewExpression(n)) {
    if (ts.isCallExpression(n) && (ts.isIdentifier(n.expression) && n.expression.text === 'eval' || n.expression.kind === k.ImportKeyword))
      g.unsupported('DYNAMIC_EXECUTION');
    children = [n.expression, ...(n.arguments || [])];
  } else if (ts.isPropertyAccessExpression(n)) children = [n.expression];
  else if (ts.isElementAccessExpression(n)) children = [n.expression, n.argumentExpression];
  else if (ts.isTypeOfExpression(n) || ts.isVoidExpression(n) || ts.isDeleteExpression(n)) children = [n.expression];
  else if (ts.isArrayLiteralExpression(n)) children = n.elements.filter(e => !ts.isOmittedExpression(e));
  else if (ts.isTemplateExpression(n)) children = n.templateSpans.map(s => s.expression);
  else g.unsupported('EXPRESSION_' + ts.SyntaxKind[n.kind]);
  for (let i = children.length - 1; i >= 0; i--) next = read(children[i], next);
  return next;
};
