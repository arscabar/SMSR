let previewKind='',previewSelected='',previewPrevious='';
function selectPreview(id){const item=previewItems.find(n=>n.id===id);if(!item)return;
  const previous=previewSelected;previewSelected=id;previewPrevious=previous===id?'':previous;
  document.querySelectorAll('.result').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.id===id)));
  showPreviewItem(item,previewPrevious);
}
function searchPreview(){
  const query=document.querySelector('#query').value.trim().toLocaleLowerCase();
  const matches=previewItems.filter(n=>(!previewKind||n.kind===previewKind)&&`${n.label} ${n.path}`.toLocaleLowerCase().includes(query));
  const results=document.querySelector('#results');results.replaceChildren();
  document.querySelector('#count').textContent=`${matches.length}개`;
  for(const item of matches){const b=previewElement('button','',`result ${item.kind}`);b.type='button';b.dataset.id=item.id;b.setAttribute('aria-pressed',String(previewSelected===item.id));
    b.append(previewElement('strong',item.label),previewElement('small',`${previewKindNames[item.kind]} · ${item.path}`));b.onclick=()=>selectPreview(item.id);results.append(b);}
  if(!matches.length)results.append(previewElement('p','결과가 없습니다. 검색어나 종류 필터를 바꿔보세요.','muted'));
  if(!matches.some(n=>n.id===previewSelected)){
    previewSelected='';previewPrevious='';document.querySelector('#selection').replaceChildren(previewElement('h2','항목을 선택하세요'));
    const graph=document.querySelector('#connections');graph.hidden=true;graph.open=false;document.querySelector('#graph').replaceChildren();
  }
}
document.querySelector('#search').onsubmit=e=>{e.preventDefault();searchPreview();};
document.querySelector('#query').addEventListener('input',searchPreview);
document.querySelector('#filters').onclick=e=>{const b=e.target.closest('button[data-kind]');if(!b)return;
  previewKind=b.dataset.kind;
  document.querySelectorAll('#filters button').forEach(n=>n.setAttribute('aria-pressed',String(n.dataset.kind===previewKind)));searchPreview();};
searchPreview();selectPreview('order');
