import {text} from './graph-explorer-trace-result.js';
export function batchPreview(box,data){
  let page=0;
  function draw(){
    box.replaceChildren(text('p',`새 분석 ${data.eligible} · 최신 재사용 ${data.current} · 제외 ${data.excluded}`));
    for(const file of data.files.slice(page*50,page*50+50))box.append(text('p',`${file.path} · ${{READY:'분석 대상',CURRENT:'최신 재사용',EXCLUDED:'제외'}[file.status]}${file.reason?' · '+file.reason:''}`));
    for(const [label,delta,disabled]of [['이전',-1,page===0],['다음',1,(page+1)*50>=data.files.length]]){
      const button=text('button',label);button.type='button';button.disabled=disabled;button.onclick=()=>{page+=delta;draw();};box.append(button);
    }
  }
  draw();
}
