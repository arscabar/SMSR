module.exports = (ts, program, input) => {
  let work = 0, rows = 0;
  function tick() {if (++work > 250000) throw Error('work limit');}
  function charge() {tick(); if (++rows > 20000) throw Error('fact limit');}
  const checker = program.getTypeChecker();
  const facts = require('./typescript_symbols.cjs')(ts, checker, tick);
  const functions = require('./typescript_functions.cjs')(ts, checker, facts, charge, tick);
  const references = [], calls = [], callNodes = [];
  function visit(n) {
    tick();
    if (ts.isIdentifier(n) || ts.isPrivateIdentifier(n)) {
      const shorthand = ts.isShorthandPropertyAssignment(n.parent) && n.parent.name === n;
      const s = shorthand ? checker.getShorthandAssignmentValueSymbol(n.parent) : checker.getSymbolAtLocation(n);
      const symbolId = facts.symbol(s);
      if (symbolId) {
        references.push({...facts.location(n), symbolId,
          role: s?.flags & ts.SymbolFlags.Alias ? 'ALIAS_REFERENCE' : !shorthand && s?.declarations?.some(d => d.name === n) ? 'DECLARATION' : 'REFERENCE'});
        rows++;
      }
    }
    if (ts.isCallExpression(n) || ts.isNewExpression(n) || ts.isJsxOpeningElement(n) || ts.isJsxSelfClosingElement(n)) {
      calls.push(require('./typescript_calls.cjs')(ts, checker, facts, n)); rows++;
      callNodes.push(n);
    }
    if (ts.isFunctionLike(n) && n.body) functions.get(n);
    if (rows + facts.symbols.size > 20000) throw Error('fact limit');
    ts.forEachChild(n, visit);
  }
  for (const source of program.getSourceFiles()) if (input.has(source.fileName)) visit(source);
  const raw = ts.getPreEmitDiagnostics(program);
  const invalid = raw.some(d => d.category === ts.DiagnosticCategory.Error);
  for (const [node, report] of functions.functions) {
    report.values = require('./typescript_values.cjs')(ts, checker, facts, charge, tick, node, invalid);
    report.control = require('./typescript_control.cjs')(ts, facts, charge, tick, node, invalid);
  }
  const connections = callNodes.map(n => {charge(); return require('./typescript_connections.cjs')(
    ts, checker, facts, functions, input, charge, n, invalid, tick);});
  require('./typescript_summaries.cjs')([...functions.functions.values()], connections, charge, tick);
  if (rows + facts.symbols.size > 20000) throw Error('fact limit');
  return {status: invalid ? 'COMPILER_CANDIDATE' : 'COMPILER_BINDINGS',
    compiler: 'TypeScript', compilerVersion: ts.version, positionEncoding: 'UTF16',
    symbols: [...facts.symbols.values()], references, calls, functions: [...functions.functions.values()], connections,
    diagnostics: raw.slice(0, 200).map(d => ({code: d.code, category: ts.DiagnosticCategory[d.category],
      path: d.file?.fileName.replace(/^\/smsr\//, ''), start: d.start, length: d.length})),
    diagnosticCount: raw.length, diagnosticsTruncated: raw.length > 200,
    limitations: ['Input/standard libraries only; no config, packages, plugins, builds or target execution.',
      'Static candidates, not runtime targets/full PDG/taint. No heap/alias guarantee.',
      'No literal values/diagnostic messages. Types summarized.',
      'No rest/spread/JSX expansion. Errors invalidate bindings. Reanalyze after compiler changes.']};
};
