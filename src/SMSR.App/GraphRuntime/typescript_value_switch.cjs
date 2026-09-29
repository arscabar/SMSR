const {copy, merge} = require('./typescript_value_state.cjs');
module.exports = (ts, g, expr, statement, n, state, controls, outer) => {
  const selector = expr(n.expression, state, controls), clauses = n.caseBlock.clauses;
  const entries = new Map(), conditions = [...controls, selector];
  let fallback = -1;
  // Search snapshots precede bodies: later case side effects cannot reach an earlier match.
  for (let i = 0; i < clauses.length; i++) {
    g.tick(); const clause = clauses[i];
    if (ts.isDefaultClause(clause)) {fallback = i; continue;}
    const value = expr(clause.expression, state, conditions);
    const test = g.node(clause.expression, 'SWITCH_CASE_COMPARISON_CANDIDATE');
    g.edge(selector, test, 'OPERAND_CANDIDATE'); g.edge(value, test, 'OPERAND_CANDIDATE');
    g.control(test, conditions); conditions.push(test);
    entries.set(i, {state: copy(state), controls: [...conditions]});
  }
  if (fallback >= 0) entries.set(fallback, {state: copy(state), controls: [...conditions]});
  let exit = fallback < 0 ? copy(state) : null, fall = null, active = [];
  const frame = {breaks: null, continues: null};
  // ponytail: path-insensitive joins match existing MAY analysis; path predicates remain separate.
  for (let i = 0; i < clauses.length; i++) {
    g.tick(); const entry = entries.get(i);
    fall = merge(fall, entry.state, g.tick);
    active = [...new Set([...active, ...entry.controls])];
    for (const child of clauses[i].statements) {
      fall = statement(child, fall, active, frame);
      if (!fall) break;
    }
    if (!fall) active = [];
  }
  if (frame.continues) {
    if (!outer) g.unsupported('LABELED_OR_NONLOOP_TRANSFER');
    outer.continues = merge(outer.continues, frame.continues, g.tick);
  }
  exit = merge(exit, fall, g.tick);
  return merge(exit, frame.breaks, g.tick);
};
