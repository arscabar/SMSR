module.exports = (ts, checker, facts, charge, tick) => {
  const nodes = new Map(), edges = new Map(), locals = new Set();
  function unsupported(reason) {const error = Error('unsupported value model'); error.reason = reason; throw error;}
  function node(ast, kind, extra = {}) {
    tick(); const anchor = facts.declaration(ast), id = anchor.id + ':' + kind;
    if (!nodes.has(id)) {charge(); nodes.set(id, {id, anchorId: anchor.id, ...facts.location(ast), kind, ...extra});}
    return id;
  }
  function edge(source, target, relation) {
    tick(); const id = JSON.stringify([source, target, relation]);
    if (!edges.has(id)) {charge(); edges.set(id, {source, target, relation});}
  }
  function symbol(n) {return facts.symbol(checker.getSymbolAtLocation(n));}
  function control(id, controls) {for (const c of controls) edge(c, id, 'CONTROL_CONDITION_CANDIDATE');}
  function write(ast, name, value, state, controls) {
    if (!ts.isIdentifier(name) || !locals.has(symbol(name))) unsupported('NONLOCAL_OR_PATTERN_WRITE');
    const symbolId = symbol(name), id = node(ast, 'ASSIGNMENT', {symbolId});
    edge(value, id, 'ASSIGNMENT_INPUT'); control(id, controls); state.set(symbolId, new Set([id])); return value;
  }
  return {nodes, edges, locals, node, edge, symbol, control, write, unsupported, tick};
};
