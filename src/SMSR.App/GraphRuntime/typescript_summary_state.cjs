module.exports = (functions, charge, tick) => {
  const states = new Map();
  for (const f of functions) {
    const v = f.values, labels = new Map(), parameters = new Map();
    f.parameters.forEach(p => parameters.set(p.id + ':PARAMETER_INPUT', p.index));
    for (const n of v.nodes) {
      tick(); const seed = parameters.has(n.id) ? [n.id] : n.kind === 'EXTERNAL_READ' ? ['unknown:' + n.id] : [];
      labels.set(n.id, new Set(seed));
    }
    states.set(f.id, {f, labels, parameters, edges: new Map(),
      returns: v.nodes.filter(n => n.kind === 'RETURN' || n.kind === 'IMPLICIT_UNDEFINED_RETURN_CANDIDATE')});
  }
  function add(target, values) {
    let changed = false;
    for (const value of values) {tick(); if (!target.has(value)) {charge(); target.add(value); changed = true;}}
    return changed;
  }
  return {states, add};
};
