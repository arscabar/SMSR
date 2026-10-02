import {text} from './graph-explorer-trace-result.js';
export async function semanticDetails(node,box,{get,projectId,sourceLink}){
  if(!['concept','requirement','rationale'].includes(node.kind))return;
  const primary=box.querySelector('.source-action');
  const part=text('details','');part.open=true;part.append(text('summary','문서 의미 분석 근거'));box.append(part);
  if(node.details?.rationale)part.append(text('blockquote',node.details.rationale));
  try{const s=await get('/api/graph/document-semantic',{projectId,path:node.sourcePath});
    part.append(text('p',s.stale?'원문·의존 코드 변경 · 다시 분석 필요':'현재 원문과 일치'),text('small',`${s.report.model} · ${s.report.contractVersion} · ${s.report.capturedAt}`));
    const evidence=s.report.nodes.find(n=>n.label===node.label&&n.quote===node.details?.rationale);
    const selectedLocation=node.details?.sourceLocation||evidence?.location;
    if(primary&&selectedLocation){const url=new URL(primary.href,location.href);url.searchParams.set('location',selectedLocation);url.hash='selected-evidence';primary.href=url.href;}
  }catch(e){part.append(text('p',e.message));}
}
