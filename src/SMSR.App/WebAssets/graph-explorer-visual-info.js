import {text} from './graph-explorer-trace-result.js';
import {relationName} from './graph-explorer-labels.js';
import {evidenceName,resolutionName} from './graph-explorer-evidence.js';
export function visualInfo(data,{get,projectId,select,sourceLink,open,focus,mark}){
  const body=document.querySelector('#visual-info');let sequence=0;
  const button=(label,fn)=>{const b=text('button',label);b.type='button';b.addEventListener('click',fn);return b;};
  async function node(id){const mine=++sequence,n=data.nodes.find(n=>n.id===id);if(!n)return;
    body.replaceChildren(text('h3',n.label));body.append(text('p',`${n.groupName}${n.cohesion!==null?` · 응집도 ${n.cohesion.toFixed(4)}`:''}`));
    if(n.source){body.append(text('small',`${n.source.sourcePath}:${n.source.line}`));const a=sourceLink(n.source);if(a)body.append(a);
      body.append(button('선택 항목 상세',()=>select(n.source)));
    }else body.append(text('p',`${n.members}개 항목`),button('그룹 안으로',()=>open(n.groupId)));
    const ids=new Set(data.edges.flatMap(e=>e.sourceId===id?[e.targetId]:e.targetId===id?[e.sourceId]:[]));
    const nearby=text('details','');nearby.open=true;nearby.append(text('summary',`연결된 항목 ${ids.size}개`));
    for(const other of data.nodes.filter(x=>ids.has(x.id)))nearby.append(button(other.label,()=>focus(other.id)));body.append(nearby);
    if(n.source)try{const lesson=await get('/api/graph/feedback-lessons',{projectId,sourceId:n.id});if(mine!==sequence||lesson.revision!==data.revision)return;
      for(const l of lesson.items){body.append(text('small',l.guidance));mark(id,l.feedback.verdict);}
      if(lesson.staleExcluded)body.append(text('small','변경된 원문의 이전 피드백은 제외했습니다.'));
    }catch(e){if(mine===sequence)body.append(text('small',e.message));}
  }
  function edge(id){++sequence;const e=data.edges.find(e=>e.id===id);if(!e)return;
    body.replaceChildren(text('h3',relationName(e.relation)),text('p',`${evidenceName(e)} · ${resolutionName(e)} · ${e.count}개 관계`));
    for(const id of [e.sourceId,e.targetId]){const n=data.nodes.find(n=>n.id===id);if(n)body.append(button(n.label,()=>focus(id)));}
    for(const proof of e.evidence){body.append(text('small',`${proof.ownerPath}:${proof.sourceLine} · ${proof.evidence?.analyzer||'근거 엔진 미기록'}`));
      const a=sourceLink({kind:/\.(md|html|pdf|docx|xlsx)$/i.test(proof.ownerPath)?'document':'code',sourcePath:proof.ownerPath,line:proof.sourceLine});if(a)body.append(a);}
  }
  return {node,edge,clear(){++sequence;body.replaceChildren();}};
}
