const fs = require('node:fs'), path = require('node:path');
module.exports = (ts, files, compiler) => {
  if (!Array.isArray(files) || files.length < 1 || files.length > 500) throw Error('files');
  const input = new Map(), seen = new Set(); let bytes = 0;
  for (const f of files) {
    if (!f || typeof f.path !== 'string' || f.path.length > 1024 ||
        /[\x00-\x1f\x7f\\:]/.test(f.path) ||
        f.path.split('/').some(p => ['', '.', '..'].includes(p)) ||
        !/\.(js|jsx|ts|tsx)$/.test(f.path) || typeof f.text !== 'string') throw Error('file');
    const size = Buffer.byteLength(f.text); bytes += size;
    if (size > 2 * 1024 ** 2 || bytes > 16 * 1024 ** 2 || seen.has(f.path.toLowerCase())) throw Error('limit');
    seen.add(f.path.toLowerCase()); input.set('/smsr/' + f.path, f.text);
  }
  const libraries = new Map();
  function read(name) {
    if (input.has(name)) return input.get(name);
    if (!/^\/smsr-lib\/lib\.[a-z0-9.-]+\.d\.ts$/.test(name)) return undefined;
    if (!libraries.has(name)) {
      const file = path.join(path.dirname(compiler), path.posix.basename(name));
      libraries.set(name, fs.existsSync(file) ? fs.readFileSync(file, 'utf8') : undefined);
    }
    return libraries.get(name);
  }
  const options = {target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext,
    moduleResolution: ts.ModuleResolutionKind.Bundler, jsx: ts.JsxEmit.Preserve,
    allowJs: true, checkJs: true, strict: true, noEmit: true, skipLibCheck: true, types: []};
  const host = {
    readFile: read, fileExists: n => read(n) !== undefined,
    getSourceFile: (n, v) => {const t = read(n); return t === undefined ? undefined : ts.createSourceFile(n, t, v, true);},
    getDefaultLibFileName: () => '/smsr-lib/lib.es2022.full.d.ts',
    getCurrentDirectory: () => '/smsr', getCanonicalFileName: n => n,
    useCaseSensitiveFileNames: () => true, getNewLine: () => '\n',
    writeFile: () => {throw Error('emit prohibited');},
    resolveModuleNames: (names, containing) => names.map(n => /^\.\.?\//.test(n) ?
      ts.resolveModuleName(n, containing, options, host).resolvedModule : undefined)
  };
  return {program: ts.createProgram([...input.keys()], options, host), input};
};
