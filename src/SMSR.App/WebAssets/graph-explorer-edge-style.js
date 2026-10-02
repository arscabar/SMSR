export function edgeStyle(edge){
  const extracted=['EXTRACTED','EXPLICIT'].includes(edge.confidence);
  const confirmed=extracted&&(!edge.resolution||['RESOLVED','FILE_ONLY'].includes(edge.resolution));
  if(confirmed)return {label:edge.resolution==='FILE_ONLY'?'파일 확인':'원문 근거',stroke:'#66a7ed',strokeDasharray:'none'};
  if(edge.confidence==='AMBIGUOUS'||(extracted&&edge.resolution!=='INFERRED'))
    return {label:'대상 후보',stroke:'#e5bd6b',strokeDasharray:'2 5'};
  return {label:extracted?'대상 추론':'추론',stroke:'#b59bdf',strokeDasharray:'8 5'};
}
