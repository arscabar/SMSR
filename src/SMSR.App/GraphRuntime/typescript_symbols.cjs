const {createHash} = require('node:crypto');
module.exports = (ts, checker, tick) => {
  const symbols = new Map();
  function location(n) {
    const source = n.getSourceFile(), start = n.getStart(source), end = n.getEnd();
    const at = source.getLineAndCharacterOfPosition(start);
    return {path: source.fileName.replace(/^\/smsr\//, ''), start, length: end - start,
      line: at.line + 1, column: at.character + 1};
  }
  function declaration(n) {
    const at = location(n);
    return {id: `${at.path}:${at.start}:${n.kind}`, kind: ts.SyntaxKind[n.kind], ...at};
  }
  function symbol(s) {
    if (!s) return null;
    if (s.flags & ts.SymbolFlags.Alias) s = checker.getAliasedSymbol(s);
    const declarations = (s.declarations || []).map(n => {tick(); return declaration(n);});
    if (!declarations.length) return null;
    const id = createHash('sha256').update(declarations.map(d => d.id).sort().join('\n')).digest('hex');
    if (!symbols.has(id)) {
      const name = s.declarations[0].name;
      symbols.set(id, {id, name: name && (ts.isIdentifier(name) || ts.isPrivateIdentifier(name)) ? name.text : '<unnamed>',
        origin: declarations.every(d => d.path.startsWith('/smsr-lib/')) ? 'STANDARD_LIBRARY' : 'INPUT', declarations});
    }
    return id;
  }
  function type(t, depth = 0) {
    tick();
    if (!t) return {kind: 'UNKNOWN'};
    if (depth > 3) return {kind: 'DEPTH_LIMIT'};
    const f = ts.TypeFlags;
    for (const [flag, kind] of [[f.StringLike, 'STRING'], [f.NumberLike, 'NUMBER'],
      [f.BooleanLike, 'BOOLEAN'], [f.BigIntLike, 'BIGINT'], [f.ESSymbolLike, 'SYMBOL'],
      [f.Any, 'ANY'], [f.Unknown, 'UNKNOWN'], [f.Void, 'VOID'], [f.Undefined, 'UNDEFINED'],
      [f.Null, 'NULL'], [f.Never, 'NEVER']]) if (t.flags & flag) return {kind};
    if (t.isUnionOrIntersection()) return {kind: t.isUnion() ? 'UNION' : 'INTERSECTION',
      members: t.types.slice(0, 16).map(v => type(v, depth + 1)), truncated: t.types.length > 16};
    return {kind: t.flags & f.TypeParameter ? 'TYPE_PARAMETER' : 'STRUCTURAL', symbolId: symbol(t.aliasSymbol || t.symbol)};
  }
  return {symbols, location, declaration, symbol, type};
};
