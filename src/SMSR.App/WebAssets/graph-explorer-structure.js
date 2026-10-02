import {text} from './graph-explorer-trace-result.js';
import {structureList} from './graph-explorer-structure-list.js';
export function structure({get,projectId,select}){
  const note=document.querySelector('#structure-status');
  let data=null,path='',sequence=0;
  async function open(item){if(item.kind==='unknown')return;
    try{if(item.node)await select(item.node);
      if(item.kind==='folder'||item.id.startsWith('file:'))await load(item.path);
    }catch(e){note.textContent=e.message;}
  }
  const list=structureList(open);
  function draw(){if(!data)return;const q=document.querySelector('#structure-filter').value.trim().toLowerCase();
    const items=data.items.filter(n=>`${n.label} ${n.path}`.toLowerCase().includes(q));
    list.show(items,data.external);
    note.textContent=`${data.path||projectId} · ${data.totalFiles}개 파일 · ${items.length}개 항목`;
  }
  async function load(next='',fresh=false){const mine=++sequence;
    note.textContent='저장된 구조 확인 중…';window.smsrLoading?.show(note,'connecting',note.textContent);
    try{const result=await get('/api/graph/structure',{projectId,path:next,...(!fresh&&data?{revision:String(data.revision)}:{})});
      if(mine!==sequence)return;data=result;path=result.path;document.querySelector('#structure-filter').value='';
      const nav=document.querySelector('#structure-breadcrumbs');nav.replaceChildren();
      const add=(label,value)=>{const b=text('button',label);b.type='button';b.addEventListener('click',()=>load(value));nav.append(b)};
      add('프로젝트 전체','');const parts=path.split('/').filter(Boolean);parts.forEach((part,i)=>add(part,parts.slice(0,i+1).join('/')));draw();
    }catch(e){if(mine===sequence){list.show([],[]);data=null;note.textContent=e.message;}}
    finally{if(mine===sequence)window.smsrLoading?.hide(note);}
  }
  document.querySelector('#structure-filter').addEventListener('input',draw);
  document.querySelector('#structure-refresh').addEventListener('click',()=>load(path,true));
  document.querySelector('#structure-panel').addEventListener('toggle',e=>{if(e.target.open&&!data)load()});
}
