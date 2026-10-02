function previewElement(tag,text,cls){const e=document.createElement(tag);e.textContent=text;if(cls)e.className=cls;return e;}
function showPreviewItem(item,backId){
  const panel=document.querySelector('#selection');panel.replaceChildren();
  if(backId){const back=previewElement('button','← 이전 항목','back');back.type='button';back.onclick=()=>selectPreview(backId);panel.append(back);}
  panel.append(previewElement('h2',item.label),previewElement('p',`${previewKindNames[item.kind]} · ${item.path}`,'muted path'),
    previewElement('p',item.role||'이 항목의 역할을 설명할 분석 결과가 아직 없습니다.','role'));
  const source=previewElement('button','원문 보기','primary source-action');source.type='button';
  source.onclick=()=>{document.querySelector('#source-title').textContent=item.label;document.querySelector('#source-text').textContent=item.source;document.querySelector('#source').showModal();};panel.append(source);
  panel.append(previewElement('h3','관련 항목'));
  const links=previewElement('div','','related');
  for(const [id,relation] of item.related){const target=previewItems.find(n=>n.id===id);const button=previewElement('button',`${target.label} · ${relation}`,target.kind);button.type='button';button.onclick=()=>selectPreview(id);links.append(button);}
  if(!item.related.length)links.append(previewElement('p','저장된 관계가 없습니다.','muted'));panel.append(links);
  const evidence=document.createElement('details');evidence.append(previewElement('summary','판단 근거'),previewElement('p',item.evidence));panel.append(evidence);
  const graph=document.querySelector('#graph');graph.replaceChildren();
  graph.append(previewElement('strong',item.label,`graph-center ${item.kind}`));
  const neighbors=previewElement('div','','graph-neighbors');
  for(const [id,relation] of item.related){const target=previewItems.find(n=>n.id===id);const button=previewElement('button',target.label,target.kind);button.type='button';button.onclick=()=>selectPreview(id);
    const row=previewElement('div','','graph-branch');row.append(previewElement('span',relation,'relation-label'),button);neighbors.append(row);}
  graph.append(neighbors);
  document.querySelector('#connections').hidden=!item.related.length;
}
