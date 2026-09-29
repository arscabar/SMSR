module.exports = (ts, checker, facts, node) => {
  const jsx = ts.isJsxOpeningElement(node) || ts.isJsxSelfClosingElement(node);
  const signature = checker.getResolvedSignature(node), declaration = signature?.declaration;
  const parameters = signature?.parameters || [];
  const args = [...(node.arguments || [])];
  const expanded = jsx || args.some(ts.isSpreadElement) ||
    parameters.some(p => p.declarations?.some(d => d.dotDotDotToken));
  return {id: facts.declaration(node).id, ...facts.location(node), kind: ts.SyntaxKind[node.kind],
    binding: declaration ? 'STATIC_SIGNATURE' : 'UNRESOLVED',
    targetDeclarationId: declaration ? facts.declaration(declaration).id : null,
    target: declaration ? facts.declaration(declaration) : null,
    returnType: signature ? facts.type(checker.getReturnTypeOfSignature(signature)) : {kind: 'UNKNOWN'},
    mapping: expanded ? 'SPREAD_REST_OR_JSX_NOT_EXPANDED' : 'POSITIONAL_SIGNATURE_ONLY',
    arguments: args.map((arg, index) => ({index, ...facts.location(arg),
      type: facts.type(checker.getTypeAtLocation(arg)),
      parameterSymbolId: expanded ? null : facts.symbol(parameters[index])})),
    parameters: parameters.map((p, index) => ({index, symbolId: facts.symbol(p),
      type: facts.type(checker.getTypeOfSymbolAtLocation(p, node))}))};
};
