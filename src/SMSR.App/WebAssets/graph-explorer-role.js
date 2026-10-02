import {text} from './graph-explorer-trace-result.js';
import {roleJob} from './graph-explorer-role-job.js';
export function citationLine(e,quote){
  if(!quote||typeof e.quote!=='string')return null;
  const at=e.quote.indexOf(quote);return at<0?null:e.line+(e.quote.slice(0,at).match(/\n/g)||[]).length;
}

export async function roleDetails(node,box,{get,projectId,sourceLink},previous){
  if(!['code','symbol'].includes(node.kind))return;
  const section=text('section','');section.className='role-card';
  section.append(text('h4','역할과 동작'));if(previous?.isConnected)previous.replaceWith(section);else box.append(section);
  const note=text('p','원문 기반 설명 확인 중…');section.append(note);
  window.smsrLoading?.show(note,'searching',note.textContent);
  try{
    const data=await get('/api/graph/role',{projectId,nodeId:node.nodeId});
    note.textContent=data.status==='CURRENT'?'':data.reason;
    if(data.status!=='CURRENT'){
      await roleJob(node,section,note,{get,projectId,refresh:()=>{if(section.isConnected)roleDetails(node,box,{get,projectId,sourceLink},section);}});
      return;
    }
    const report=data.report, evidence=text('details','');evidence.append(text('summary','설명 근거 확인'));
    for(const claim of report.claims){
      const card=text('div','');card.className='role-claim';
      const body=claim.kind==='ROLE'?claim.text.replace(/^ROLE:\s*/,''):claim.text;
      card.append(text('strong',({ROLE:'역할',BEHAVIOR:'동작',RELATION:'관계',RATIONALE:'설계 이유',LIMITATION:'확인되지 않은 부분'})[claim.kind]+(claim.confidence==='INFERRED'?' · 추정':'')),text('p',body));section.append(card);
      const item=text('div','');item.append(text('blockquote',claim.supportingQuote));
      for(const id of claim.evidenceIds){const e=report.evidence.find(e=>e.id===id);if(!e)continue;
        const line=citationLine(e,claim.supportingQuote);if(line===null)continue;
        item.append(text('small',`${e.path}:${line}`));const link=sourceLink({kind:'code',sourcePath:e.path,line});if(link)item.append(link);
      }evidence.append(item);
    }
    evidence.append(text('small',`${report.model} · ${report.createdAt} · 정적 근거이며 실행 순서가 아닙니다.`));section.append(evidence);
  }catch(e){note.textContent=e.message;}
  finally{window.smsrLoading?.hide(note);}
}
