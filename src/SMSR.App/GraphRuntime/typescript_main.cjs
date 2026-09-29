const fs = require('node:fs');
try {
  const chunks = []; let size = 0;
  for (;;) {
    const buffer = Buffer.alloc(65536), n = fs.readSync(0, buffer, 0, buffer.length, null);
    if (!n) break;
    size += n; if (size > 32 * 1024 ** 2) throw Error('input limit');
    chunks.push(buffer.subarray(0, n));
  }
  const compiler = process.argv[2], ts = require(compiler);
  if (!/^6\.0\./.test(ts.version)) throw Error('unsupported compiler API');
  const {program, input} = require('./typescript_host.cjs')(ts,
    JSON.parse(Buffer.concat(chunks).toString('utf8')).files, compiler);
  const result = require('./typescript_facts.cjs')(ts, program, input);
  const output = JSON.stringify(result);
  if (Buffer.byteLength(output) > 16 * 1024 ** 2) throw Error('output limit');
  process.stdout.write(output);
} catch {
  process.stderr.write('Local TypeScript analysis failed'); process.exitCode = 1;
}
