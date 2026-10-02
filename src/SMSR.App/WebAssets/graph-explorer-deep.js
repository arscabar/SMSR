const text=(tag,value)=>{const n=document.createElement(tag);n.textContent=value;return n};
const analyzer={analysis:'Python',csharp:'C#',java:'Java',typescript:'TypeScript',jdt:'Java JDT'};
const status={APPLIED:'관계에 통합됨',NO_UNIQUE_CALL_MAPPING:'유일한 심벌 대응 없음',INPUT_CHANGED_OR_REMOVED:'원문 변경·제거',CONFIGURATION_CHANGED:'설정 변경',INDEX_SCOPE_CHANGED:'색인 범위 변경',ANALYZER_VERSION_CHANGED:'분석기 버전 변경',LEGACY_CONTEXT_REANALYZE:'이전 결과 · 재분석 필요',INVALID_CONTEXT:'근거 정보 없음',INVALID_ANALYSIS_REPORT:'결과 형식 확인 필요',ANALYSIS_SCOPE_MISMATCH:'분석 범위 불일치'};

export function deepEvidence(panel,edge){
  if(!edge.analysis?.length)return false;
  const box=text('details','');box.append(text('summary',`심층 분석 근거 ${edge.analysis.length}개`));
  for(const item of edge.analysis){
    const card=text('div','');card.className='trace-step';
    card.append(text('strong',`${analyzer[item.analyzer]||item.analyzer} · 분석 버전 ${item.analysisVersion}`),
      text('p',`대상 대응: ${item.binding} · 분기 판정: ${item.dispatch}`),
      text('small',`분석 범위: ${item.analysisKey} · 프로필: ${item.profile}`),
      text('small',`원문 지문: ${item.inputHash?.slice(0,12)||'없음'} · 설정 지문: ${item.settingsHash?.slice(0,12)||'없음'}`));
    box.append(card);
  }
  panel.append(box,text('small','저장된 심층 정적 분석 근거 · 실제 실행 순서가 아닙니다.'));
  return true;
}

export async function deepStatus(box,get,projectId){
  try{
    const items=await get('/api/graph/deep-status',{projectId});
    const groups=new Map();
    for(const item of items)groups.set(item.status,(groups.get(item.status)||0)+1);
    box.replaceChildren(text('strong','별도 심층 분석 결과'));
    if(!items.length){box.append(text('p','저장된 심층 결과 없음 · 기본 정적 색인 사용'));return;}
    for(const [key,count]of groups)box.append(text('p',`${status[key]||'결과 확인 필요'} · ${count}건`));
  }catch(e){box.replaceChildren(text('p',`심층 결과 상태 확인 실패: ${e.message}`));}
}
