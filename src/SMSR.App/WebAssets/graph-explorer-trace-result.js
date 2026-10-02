import {relationName} from './graph-explorer-labels.js';
import {evidenceName,resolutionName} from './graph-explorer-evidence.js?v=2';

export function text(tag,value){const n=document.createElement(tag);n.textContent=value;return n;}
export function traceResult(data,panel,select,sourceLink){
  panel.replaceChildren(text('h3',`${data.start.label} → ${data.target.label}`));
  panel.append(text('small',`색인 ${data.revision} · 정적 연결 경로 · 실제 실행 순서가 아님`));
  if(!data.found){panel.append(text('p',data.truncated?'탐색 깊이·건수 제한에 도달했습니다. 경로 없음으로 단정할 수 없습니다.':'현재 범위·관계 조건에서 연결 경로를 찾지 못했습니다. 추론 포함 여부와 색인 진단을 확인하세요.'));return;}
  if(!data.steps.length){panel.append(text('p','시작과 도착이 같은 항목입니다.'));return;}
  for(const step of data.steps){
    const card=text('div','');card.className='trace-step';
    for(const node of [step.source,step.target]){
      if(node===step.target)card.append(text('span',` → ${relationName(step.edge.relation)} → `));
      const button=text('button',node.label);button.type='button';button.addEventListener('click',()=>select(node));card.append(button);
    }
    card.append(text('small',`${evidenceName(step.edge)} · ${resolutionName(step.edge)} · ${step.edge.ownerPath}:${step.edge.sourceLine}`));
    const link=sourceLink({kind:'code',sourcePath:step.edge.ownerPath,line:step.edge.sourceLine});if(link)card.append(link);
    panel.append(card);
  }
}
