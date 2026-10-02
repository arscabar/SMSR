import {text} from './graph-explorer-trace-result.js';
export function visualAnalysis(data,{get,projectId,select}){
  const core=document.querySelector('#overview-core'),insights=document.querySelector('#overview-insights');core.replaceChildren();insights.replaceChildren();
  for(const n of data.core||[]){const b=text('button',`${n.label} · ${n.degree}`);b.type='button';
    b.addEventListener('click',async()=>{try{const c=await get('/api/graph/context',{projectId,nodeId:n.nodeId});if(c.revision!==data.revision)throw Error('리비전이 변경되었습니다. 다시 확인하세요.');select(c.node);}catch(e){insights.append(text('p',e.message));}});core.append(b);}
  for(const s of data.surprises||[])insights.append(text('p',`${s.reason} · ${s.edge.ownerPath}:${s.edge.sourceLine}`));
  for(const q of data.questions||[])insights.append(text('p',q));
}
export function visualStatus(data,projectId,group){
  document.querySelector('#overview-status').textContent=`${group===null?projectId:'선택 그룹'} · ${data.nodes.length}개 ${data.mode==='communities'?'그룹':'항목'} · ${data.edges.length}개 관계${data.mode==='communities'?` · ${data.totalNodes.toLocaleString()}개 항목을 그룹으로 축약`:''}`;
  document.querySelector('#visual-limits').textContent=`${data.limitation} 연결 없는 항목 ${data.isolatedNodes}개. 지도 밖 항목 ${data.omittedNodes}개·관계 ${data.omittedEdges}개${data.omittedHyperedges?'·하이퍼엣지 1000개 초과':''}. 원본은 색인에 보존됩니다.`;
}
