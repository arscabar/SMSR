export function selectionExport(node,box,projectId){
 const panel=document.createElement('details'),summary=document.createElement('summary');summary.textContent='선택한 항목 주변 내보내기';panel.append(summary);
 for(const [format,label]of [['json','JSON'],['html','HTML'],['svg','SVG'],['graphml','GraphML'],['wiki','Wiki'],['obsidian','Obsidian']]){
  const link=document.createElement('a');link.textContent=label;link.style.marginRight='10px';link.href='/api/graph/knowledge-export?'+new URLSearchParams({projectId,nodeId:node.nodeId,depth:2,format});panel.append(link);
 }
 const note=document.createElement('small');note.textContent='양방향 두 단계 · 범위 상한은 파일에 표시됩니다.';panel.append(note);box.append(panel);
}
