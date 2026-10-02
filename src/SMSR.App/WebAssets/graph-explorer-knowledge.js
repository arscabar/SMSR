export function knowledge({get,projectId,select}) {
 const el=id=>document.getElementById(id);let watch=null;
 const text=(tag,value)=>{const n=document.createElement(tag);n.textContent=value;return n;};
 async function post(path,data){const reply=await fetch(path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(data)});const result=await reply.json();if(!reply.ok)throw Error(result.error||'요청 실패');return result;}
 el('question-form').onsubmit=async event=>{
  event.preventDefault();el('question-status').textContent='관련 근거 찾는 중…';
  try{const request={projectId,question:el('question').value,maxTokens:4000};
   const result=await post('/api/graph/question-evidence',request);
   el('question-hits').replaceChildren(...result.nodes.map(node=>{const button=text('button',node.label+' · '+node.sourcePath);button.type='button';button.onclick=()=>select(node);return button;}));
   el('question-status').textContent=`근거 ${result.nodes.length}개 · ${result.truncated?'일부 표시 · ':''}저장 리비전 ${result.revision} · 답변이 아닌 근거 목록`;
  }catch(error){el('question-status').textContent=error.message;}
 };
 el('watch-toggle').onclick=async()=>{
  const button=el('watch-toggle');button.disabled=true;
  try{watch=watch===null?await get('/api/graph/watch',{projectId}):await post('/api/graph/watch',{projectId,enabled:!watch.enabled});
   button.textContent=watch.enabled?'변경 감시 끄기':'변경 감시 켜기';el('watch-state').textContent=watch.status+(watch.error?' · '+watch.error:'');
  }catch(error){el('watch-state').textContent=error.message;}finally{button.disabled=false;}
 };
 el('report-load').onclick=async()=>{
  const panel=el('report-result');panel.textContent='구조 보고서 확인 중…';
  try{const report=await get('/api/graph/report',{projectId});
   if(report.status!=='READY'){panel.textContent=report.status==='ANALYSIS_PENDING'?'분석 중입니다. 잠시 후 보고서 확인을 다시 누르세요.':(report.limits||['분석 실패: 범위·도구·원문을 확인하세요.']).join(' ');return;}
   panel.replaceChildren(text('h3',`리비전 ${report.revision} · 구조와 근거`),text('p',`${report.overview.totalGroups}개 그룹 · ${report.coverage.totalFiles}개 파일${report.truncated?' · 일부 상세 표시':''}`));
   for(const item of report.overview.core)panel.append(text('p',item.label+' · '+item.reason));
   for(const reason of report.rationale)panel.append(text('p',reason.label+' · '+reason.path+':'+reason.line+' · '+(reason.quote||'')));
   panel.append(text('small',report.limits.join(' ')));
  }catch(error){panel.textContent=error.message;}
 };
 for(const [format,label]of [['json','JSON'],['html','독립 HTML'],['svg','SVG'],['graphml','GraphML'],['wiki','Wiki'],['obsidian','Obsidian']]){
  const link=text('a',label);link.href='/api/graph/knowledge-export?'+new URLSearchParams({projectId,format});link.style.marginRight='14px';el('knowledge-export').append(link);
 }
}
