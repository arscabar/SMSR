module.exports = (ts, checker, facts, charge, tick) => {
  const functions = new Map();
  function get(node) {
    if (functions.has(node)) return functions.get(node);
    const mode = node.asteriskToken ? 'GENERATOR' : ts.isConstructorDeclaration(node) ? 'CONSTRUCTOR' :
      node.modifiers?.some(m => m.kind === ts.SyntaxKind.AsyncKeyword) ? 'ASYNC' : 'NORMAL';
    const parameters = node.parameters.filter(p => !(ts.isIdentifier(p.name) && p.name.text === 'this'))
      .map((p, index) => {
        charge(); return {index, ...facts.declaration(p),
          symbolId: ts.isIdentifier(p.name) ? facts.symbol(checker.getSymbolAtLocation(p.name)) : null,
          pattern: ts.isIdentifier(p.name) ? 'IDENTIFIER' : 'DESTRUCTURING', rest: !!p.dotDotDotToken,
          defaultSource: p.initializer ? facts.declaration(p.initializer) : null};
      });
    const returns = [];
    function visit(n) {
      tick(); if (ts.isFunctionLike(n)) return;
      if (ts.isReturnStatement(n)) {
        charge(); returns.push({...facts.declaration(n),
          value: n.expression ? facts.declaration(n.expression) : null,
          kind: n.expression ? 'EXPLICIT_VALUE' : 'EXPLICIT_UNDEFINED'});
      }
      ts.forEachChild(n, visit);
    }
    if (ts.isBlock(node.body)) visit(node.body);
    else {
      charge(); returns.push({...facts.declaration(node.body), value: facts.declaration(node.body), kind: 'ARROW_VALUE'});
    }
    charge(); const result = {...facts.declaration(node), name: node.name && ts.isIdentifier(node.name) ? node.name.text : null, mode, parameters, returns,
      fallthrough: ts.isBlock(node.body) ? 'NOT_ANALYZED' : 'EXPRESSION_BODY',
      returnEvidence: 'SYNTAX_ONLY_NOT_REACHABILITY_OR_FINALLY_EFFECTS'};
    functions.set(node, result); return result;
  }
  return {get, functions};
};
