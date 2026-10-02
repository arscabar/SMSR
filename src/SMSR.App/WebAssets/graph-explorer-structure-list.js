import {text} from './graph-explorer-trace-result.js';
export function structureList(open){
  const body=document.querySelector('#structure-list');let items=[],external=[],offset=0;
  function render(){body.replaceChildren();
    const values=[...items,...external.map(n=>({...n,outside:true}))],end=Math.min(values.length,offset+50);
    document.querySelector('#structure-list-title').textContent=`이 영역의 모든 항목 ${items.length}개 · 바깥 연결 ${external.length}개`;
    for(const item of values.slice(offset,end)){
      const b=text('button',`${item.outside?'바깥 연결 · ':''}${item.label}`);b.type='button';
      b.append(text('small',item.path||'출처 없음'));b.addEventListener('click',()=>open(item));body.append(b);
    }
    if(!values.length)body.append(text('p','일치하는 항목이 없습니다.'));
    if(values.length>50){const nav=text('nav');nav.setAttribute('aria-label','전체 항목 목록 페이지');
      for(const [label,delta,enabled]of [['이전 항목',-50,offset>0],['다음 항목',50,end<values.length]]){
        const b=text('button',label);b.type='button';b.disabled=!enabled;b.addEventListener('click',()=>{offset+=delta;render()});nav.append(b)}
      nav.append(text('span',`${offset+1}–${end} / ${values.length}`));body.append(nav);}
  }
  return {show(next,outside){items=next;external=outside;offset=0;render()}};
}
