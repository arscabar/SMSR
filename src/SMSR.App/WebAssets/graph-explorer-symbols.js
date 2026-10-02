import {text} from './graph-explorer-trace-result.js';

export function symbolTree(node,box,{get,projectId,select}){
  if(!['code','symbol'].includes(node.kind))return;
  const panel=text('details','');panel.className='role-card';
  panel.append(text('summary','구성요소 · 클래스·함수'));box.append(panel);
  let loaded=false;
  panel.addEventListener('toggle',()=>{if(panel.open&&!loaded){loaded=true;load(node,panel,'',new Set([node.nodeId]));}});
  async function load(parent,container,prefix,seen,offset=0,revision){
    const note=text('p','구성요소 확인 중…');container.append(note);
    try{
      const data=await get('/api/graph/symbols',{projectId,nodeId:parent.nodeId,offset,...(revision?{revision}:{})});
      note.textContent=data.nodes.length?'':'하위 구성요소가 없습니다.';
      for(const item of data.nodes){
        const name=item.label.replace(/^\./,'');const owner=prefix||(!item.details?.ownerNodeId?.startsWith('file:')?data.owners?.[item.details?.ownerNodeId]:'');
        const label=owner?`${owner}.${name}`:name;
        const button=text('button',label);button.type='button';button.className='related-item';
        button.onclick=()=>select({...item,label});
        const row=text('div','');row.append(button,text('small',`${item.details?.entityKind||'심벌'} · ${item.line}행`));container.append(row);
        if(['class','interface','struct','enum','module','namespace','trait'].includes(item.details?.entityKind)&&!seen.has(item.nodeId)){
          const children=text('details','');children.append(text('summary','내부 구성요소'));row.append(children);
          let opened=false;children.addEventListener('toggle',()=>{if(children.open&&!opened){opened=true;load(item,children,label,new Set([...seen,item.nodeId]),0,data.revision);}});
        }
      }
      if(data.truncated){const more=text('button','구성요소 더 보기');more.type='button';container.append(more);
        more.onclick=()=>{more.remove();load(parent,container,prefix,seen,offset+30,data.revision);};}
    }catch(error){note.textContent=error.message;const retry=text('button','다시 확인');retry.type='button';container.append(retry);
      retry.onclick=()=>{note.remove();retry.remove();load(parent,container,prefix,seen,offset,revision);};}
  }
}
