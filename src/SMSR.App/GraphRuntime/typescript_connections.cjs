module.exports = (ts, checker, facts, functions, input, charge, node, invalid, tick) => {
  const callId = facts.declaration(node).id;
  let owner = node.parent;
  while (owner && !ts.isFunctionLike(owner)) owner = owner.parent;
  const caller = owner?.body ? functions.get(owner) : null;
  const valueId = (f, anchor, kind) => f?.values?.nodes.find(n => {
    tick(); return n.anchorId === anchor && n.kind === kind;
  })?.id || null;
  const result = {callId, callerFunctionId: owner?.body ? facts.declaration(owner).id : null,
    callValueId: valueId(caller, callId, 'OPAQUE_CALL_RESULT'),
    bodyId: null, status: 'UNAVAILABLE', reason: null, inputs: [], returns: []};
  const signature = checker.getResolvedSignature(node), selected = signature?.declaration;
  const candidates = selected?.body ? [selected] : selected?.name ?
    (checker.getSymbolAtLocation(selected.name)?.declarations || []).filter(d => ts.isFunctionLike(d) && d.body) : [];
  const bodies = candidates.filter(d => input.has(d.getSourceFile().fileName));
  if (bodies.length !== 1) {
    result.reason = bodies.length ? 'AMBIGUOUS_BODY' : 'NO_INPUT_BODY'; return result;
  }
  const body = functions.get(bodies[0]); result.bodyId = body.id;
  const args = [...(node.arguments || [])];
  result.reason = invalid ? 'COMPILATION_ERRORS' : !ts.isCallExpression(node) ? 'NON_CALL_EXPRESSION' :
    body.mode !== 'NORMAL' ? body.mode + '_RESULT_PROTOCOL' : args.some(ts.isSpreadElement) ? 'SPREAD_ARGUMENTS' :
    body.parameters.some(p => p.rest || p.pattern !== 'IDENTIFIER') ? 'REST_OR_DESTRUCTURING_PARAMETERS' : null;
  if (result.reason) return result;
  result.status = 'STATIC_BODY_CANDIDATE';
  for (const p of body.parameters) {
    charge(); const arg = args[p.index];
    result.inputs.push({parameterId: p.id, parameterSymbolId: p.symbolId, index: p.index,
      parameterValueId: valueId(body, p.id, 'PARAMETER_INPUT'),
      argumentValueId: arg ? valueId(caller, facts.declaration(arg).id, 'CALL_ARGUMENT_' + p.index) : null,
      argument: arg ? facts.declaration(arg) : null,
      relation: arg ? 'ARGUMENT_TO_PARAMETER_CANDIDATE' : 'OMITTED_ARGUMENT_UNDEFINED',
      defaultSource: p.defaultSource, defaultCondition: p.defaultSource ? 'WHEN_ARGUMENT_IS_UNDEFINED' : null});
  }
  for (const r of body.returns) {
    charge(); result.returns.push({returnId: r.id, source: r.value || r, callId,
      returnValueId: valueId(body, r.id, 'RETURN'), callValueId: result.callValueId,
      relation: 'RETURN_TO_CALL_CANDIDATE', valueKind: r.kind});
  }
  result.boundary = 'No runtime target, reachability, finally, receiver, heap or alias guarantee; no argument-to-return shortcut.';
  return result;
};
