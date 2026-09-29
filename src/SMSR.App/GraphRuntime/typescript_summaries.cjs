module.exports = (functions, connections, charge, tick) => {
  const {states, add} = require('./typescript_summary_state.cjs')(functions, charge, tick);
  const calls = [];
  for (const c of connections) {
    tick(); const caller = states.get(c.callerFunctionId), body = states.get(c.bodyId);
    if (!caller?.labels.has(c.callValueId)) continue;
    const valid = c.status === 'STATIC_BODY_CANDIDATE' && body?.f.values.status === 'VALUE_FLOW_CANDIDATES';
    if (valid) calls.push({c, caller, body});
    else add(caller.labels.get(c.callValueId), ['unknown:' + c.callValueId]);
  }
  // ponytail: bounded whole-bundle fixed point; use a worklist if measured bundles hit the work cap.
  let changed;
  do {
    changed = false;
    for (const s of states.values()) for (const e of s.f.values.edges) {
      tick(); if (e.relation !== 'CONTROL_CONDITION_CANDIDATE')
        changed = add(s.labels.get(e.target), s.labels.get(e.source)) || changed;
    }
    for (const {c, caller, body} of calls) for (const r of body.returns) {
      tick();
      for (const label of body.labels.get(r.id)) {
        tick(); const index = body.parameters.get(label), target = caller.labels.get(c.callValueId);
        if (index === undefined) {changed = add(target, [label]) || changed; continue;}
        const input = c.inputs.find(i => {tick(); return i.index === index;});
        if (!input?.argumentValueId) continue;
        changed = add(target, caller.labels.get(input.argumentValueId)) || changed;
        const key = c.callId + ':' + index;
        if (!caller.edges.has(key)) {
          charge(); caller.edges.set(key, {callId: c.callId, bodyId: c.bodyId, parameterIndex: index,
            source: input.argumentValueId, target: c.callValueId, relation: 'BODY_DATA_DEPENDENCY_CANDIDATE'});
        }
      }
    }
  } while (changed);
  for (const s of states.values()) {
    s.f.valueSummary = {status: s.f.values.status === 'VALUE_FLOW_CANDIDATES' ? 'DATA_DEPENDENCY_CANDIDATES' : 'UNAVAILABLE',
      reason: s.f.values.reason || null, callEdges: [...s.edges.values()],
      returns: s.returns.map(r => {
        charge(); const labels = [...s.labels.get(r.id)];
        return {returnValueId: r.id, parameterIndices: labels.filter(x => s.parameters.has(x)).map(x => s.parameters.get(x)).sort((a,b) => a-b),
          unknownValueIds: labels.filter(x => x.startsWith('unknown:')).map(x => x.slice(8)).sort()};
      })};
  }
};
