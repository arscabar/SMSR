import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';

const code = await readFile(new URL('../src/SMSR.App/WebAssets/smsr-dashboard-disclosures.js', import.meta.url), 'utf8');
const item = key => ({dataset:{disclosure:key}, open:false, matches:selector=>selector==='details[data-disclosure]'});
let nodes = [item('activity'), item('evidence')];
const storage = new Map(), handlers = new Map();
const context = {
  document:{querySelector:()=>({dataset:{project:'SMSR', workflow:'UI:1'}}),
    querySelectorAll:()=>nodes, addEventListener:(name, fn)=>handlers.set(name,fn)},
  window:{}, sessionStorage:{getItem:key=>storage.get(key), setItem:(key,value)=>storage.set(key,value)}
};
vm.runInNewContext(code, context);
assert.deepEqual(nodes.map(n=>n.open), [false,false]);
nodes[1].open = true;
handlers.get('toggle')({target:nodes[1]});
nodes = [item('activity'), item('evidence')];
context.window.smsrDisclosures.restore();
assert.deepEqual(nodes.map(n=>n.open), [false,true]);
vm.runInNewContext(code, context);
assert.deepEqual(nodes.map(n=>n.open), [false,true]);
nodes[1].open = false;
context.window.smsrDisclosures.capture();
nodes = [item('activity'), item('evidence')];
context.window.smsrDisclosures.restore();
assert.deepEqual(nodes.map(n=>n.open), [false,false]);
context.sessionStorage.getItem = () => {throw Error('storage unavailable');};
context.sessionStorage.setItem = () => {throw Error('storage unavailable');};
nodes[0].open = true;
context.window.smsrDisclosures.capture();
nodes = [item('activity'), item('evidence')];
context.window.smsrDisclosures.restore();
assert.deepEqual(nodes.map(n=>n.open), [true,false]);
context.sessionStorage.getItem = key => storage.get(key);
context.sessionStorage.setItem = (key,value) => storage.set(key,value);
storage.clear();
nodes = [item('context'), item('detail'), item('activity')];
nodes[0].dataset.defaultOpen = 'true'; nodes[1].dataset.defaultOpen = 'true';
vm.runInNewContext(code, context);
assert.deepEqual(nodes.map(n=>n.open), [true,true,false]);
nodes[0].open = false;
context.window.smsrDisclosures.capture();
nodes = [item('context'), item('detail'), item('activity')];
nodes[0].dataset.defaultOpen = 'true'; nodes[1].dataset.defaultOpen = 'true';
context.window.smsrDisclosures.restore();
assert.deepEqual(nodes.map(n=>n.open), [false,true,false]);
const live = await readFile(new URL('../src/SMSR.App/Mvp/DashboardLiveUpdates.cs', import.meta.url),'utf8');
assert(live.indexOf('smsrDisclosures?.capture()') < live.indexOf("document.querySelector('main')?.replaceWith"));
assert(live.indexOf('smsrDisclosures?.restore()') > live.indexOf("document.querySelector('main')?.replaceWith"));
console.log('Dashboard default fold, streamed replacement, reload, blocked storage checks OK');
