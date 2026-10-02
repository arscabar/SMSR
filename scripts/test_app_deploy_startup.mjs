import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';

const source=readFileSync(new URL('../src/SMSR.App/App.xaml.cs',import.meta.url),'utf8');
const update='CheckForUpdatesOnStartupAsync';
assert.equal(source.split(update).length-1,1,'Startup update must use a single guarded call');
assert(/if \(!e\.Args\.Contains\("--skip-update-check", StringComparer\.OrdinalIgnoreCase\)\)\s*_ = viewModel\.Settings\.CheckForUpdatesOnStartupAsync\(\);/.test(source));
assert(source.includes('settings.Current.AutomateCodexIntegration && !e.Args.Contains("--skip-codex-integration", StringComparer.OrdinalIgnoreCase)'));
console.log('PASS: deployment startup guards, case-insensitive opt-out, normal startup preserved');
