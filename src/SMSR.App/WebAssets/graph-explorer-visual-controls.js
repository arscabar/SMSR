import {text} from './graph-explorer-trace-result.js';
import {groupColor} from './graph-explorer-visual-data.js';
export function visualControls(data,graph){
  const legend=document.querySelector('#visual-legend'),results=document.querySelector('#visual-results');
  const input=document.querySelector('#visual-search'),all=document.querySelector('#visual-all'),hidden=new Set();
  const counts=new Map();for(const n of data.nodes)counts.set(n.groupId,(counts.get(n.groupId)||0)+1);
  const groups=[...new Map(data.nodes.map(n=>[n.groupId,{id:n.groupId,name:n.groupName,
    count:data.mode==='communities'?n.members:counts.get(n.groupId)}])).values()];
  function state(){all.checked=!hidden.size;all.indeterminate=hidden.size>0&&hidden.size<groups.length;graph.hide(hidden);}
  function legendRows(){const q=input.value.trim().toLowerCase();legend.replaceChildren();
    for(const g of groups.filter(g=>g.name.toLowerCase().includes(q))){const label=text('label',''),cb=document.createElement('input'),dot=text('span','');
      cb.type='checkbox';cb.checked=!hidden.has(g.id);cb.addEventListener('change',()=>{cb.checked?hidden.delete(g.id):hidden.add(g.id);state();});
      dot.className='visual-dot';dot.style.background=groupColor(g.id);label.append(cb,dot,text('span',g.name),text('small',String(g.count)));legend.append(label);}
  }
  function search(){const q=input.value.trim().toLowerCase();results.replaceChildren();
    if(q){const found=data.nodes.filter(n=>`${n.label} ${n.source?.sourcePath||''}`.toLowerCase().includes(q));
      results.append(text('small',`${found.length}개 일치 · 처음 20개 바로 이동`));
      for(const n of found.slice(0,20)){const b=text('button',n.label);b.type='button';b.title=n.source?.sourcePath||n.groupName;
        b.addEventListener('click',()=>{hidden.delete(n.groupId);state();legendRows();graph.focus(n.id);});results.append(b);}
    }legendRows();
  }
  input.oninput=search;all.onchange=()=>{hidden.clear();if(!all.checked)groups.forEach(g=>hidden.add(g.id));state();legendRows();};
  input.value='';all.checked=true;all.indeterminate=false;search();
  return ()=>{input.oninput=null;all.onchange=null;};
}
