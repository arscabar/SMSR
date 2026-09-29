exports.copy = state => state && new Map([...state].map(([k, v]) => [k, new Set(v)]));
exports.merge = (a, b, tick) => {
  const result = exports.copy(a || b); if (!a || !b) return result;
  for (const [key, values] of b) for (const value of values) {
    tick(); if (!result.has(key)) result.set(key, new Set()); result.get(key).add(value);
  }
  return result;
};
exports.equal = (a, b, tick) => a.size === b.size && [...a].every(([k, values]) =>
  values.size === b.get(k)?.size && [...values].every(v => {tick(); return b.get(k).has(v);}));
