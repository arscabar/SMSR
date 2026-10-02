import { relationName } from './graph-explorer-labels.js';
import { deepEvidence } from './graph-explorer-deep.js';

export const evidenceName = edge => ['EXTRACTED','EXPLICIT'].includes(edge.confidence) ? '원문 근거' : edge.confidence === 'AMBIGUOUS' ? '모호한 관계' : '추론한 관계';
export const resolutionName = edge => ({RESOLVED:'해소된 정적 관계',FILE_ONLY:'파일까지만 확인',INFERRED:'추론한 대상'})[edge.resolution] || '미해소';

export function showEvidenceList(panel,edges,a,b,sourceLink){
  showEvidence(panel,{edge:edges[0],node:b},a,sourceLink);
  if(edges.length<2)return;
  for(const edge of edges){
    const choice=document.createElement('button');choice.textContent=`${edge.ownerPath}:${edge.sourceLine} · ${evidenceName(edge)}`;
    choice.addEventListener('click',()=>showEvidence(panel,{edge,node:b},a,sourceLink));panel.append(choice);
  }
}
export function showEvidence(panel, item, center, sourceLink) {
  const text = (tag, value) => { const node=document.createElement(tag); node.textContent=value; return node; };
  const edge = item.edge;
  panel.replaceChildren(text('h3', relationName(edge.relation)));
  const from = item.left ? item.node.label : center.label, to = item.left ? center.label : item.node.label;
  panel.append(text('p', `${from} → ${to}`), text('strong', evidenceName(edge)),text('p',edge.resolution?resolutionName(edge):'대상 해소 정보 없음'),
    text('p', `${edge.ownerPath}:${edge.sourceLine}`));
  const link=sourceLink({kind:'code',sourcePath:edge.ownerPath,line:edge.sourceLine});
  if (link) panel.append(link);
  if (!deepEvidence(panel,edge) && (edge.sourceId.startsWith('symbol:') || edge.targetId.startsWith('symbol:')))
    panel.append(text('small','Graphify 정적 분석 · 실제 실행 순서는 아닙니다.'));
}
