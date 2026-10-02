import {text} from './graph-explorer-trace-result.js';
export function groupMembers({get,projectId,select}){
  const body=document.querySelector('#overview-members');let group=null,revision=0,offset=0,sequence=0;
  async function load(){const mine=++sequence;body.replaceChildren(text('p','그룹 확인 중…'));
    try{const data=await get('/api/graph/group-members',{projectId,groupId:String(group),revision:String(revision),offset:String(offset),limit:'50'});if(mine!==sequence)return;
      body.replaceChildren(text('p',`${offset+1}–${offset+data.nodes.length} / ${data.total}개`));
      for(const n of data.nodes){const b=text('button',n.label);b.type='button';b.title=n.sourcePath;b.addEventListener('click',()=>select(n));body.append(b);}
      for(const [label,enabled,delta] of [['이전 항목',offset>0,-50],['다음 항목',data.truncated,50]]){const b=text('button',label);b.type='button';b.disabled=!enabled;b.addEventListener('click',()=>{offset+=delta;load()});body.append(b);}
    }catch(e){if(mine===sequence)body.replaceChildren(text('p',e.message));}}
  return {open(id,rev){group=id;revision=rev;offset=0;load()},clear(){++sequence;body.replaceChildren()}};
}
