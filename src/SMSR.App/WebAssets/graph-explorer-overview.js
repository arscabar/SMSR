import {visualNetwork} from './graph-explorer-visual-network.js';
import {visualControls} from './graph-explorer-visual-controls.js';
import {visualInfo} from './graph-explorer-visual-info.js';
import {visualAnalysis,visualStatus} from './graph-explorer-visual-analysis.js';
import {visualExpand} from './graph-explorer-visual-expand.js';
export function overview({get,projectId,select,sourceLink}){
  const map=document.querySelector('#visual-map'),note=document.querySelector('#overview-status');
  let data=null,graph=null,info=null,sequence=0,group=null,dispose=()=>{};
  async function load(next=null,fresh=false){const mine=++sequence;note.textContent='저장된 관계 구조 확인 중…';window.smsrLoading?.show(note,'connecting',note.textContent);
    try{const result=await get('/api/graph/visual',{projectId,...(next!==null?{groupId:String(next)}:{}),...(!fresh&&data?{revision:String(data.revision)}:{})});if(mine!==sequence)return;
      dispose();info?.clear();graph?.destroy();data=result;group=next;
      const callbacks={get,projectId,select,sourceLink,open:id=>load(id),focus:id=>graph.focus(id),mark:(id,status)=>graph.mark(id,status)};
      info=visualInfo(data,callbacks);graph=visualNetwork(map,data,{node:info.node,edge:info.edge,open:id=>{const n=data.nodes.find(n=>n.id===id);if(n&&!n.source)load(n.groupId);else if(n?.source)select(n.source);}});
      dispose=visualControls(data,graph);document.querySelector('#visual-home').disabled=next===null;
      visualStatus(data,projectId,next);visualAnalysis(data,{get,projectId,select});
      if(!data.nodes.length)note.textContent='표시할 항목이 없습니다. 색인 범위를 확인하세요.';
    }catch(e){if(mine===sequence){dispose();info?.clear();graph?.destroy();graph=null;map.replaceChildren();note.textContent=e.message;}}
    finally{if(mine===sequence)window.smsrLoading?.hide(note);}
  }
  document.querySelector('#visual-home').onclick=()=>load(null,true);
  document.querySelector('#overview-retry').onclick=()=>load(group,true);
  document.querySelector('#visual-fit').onclick=()=>graph?.fit();document.querySelector('#visual-reset').onclick=()=>graph?.reset();
  document.querySelector('#visual-in').onclick=()=>graph?.zoom(1.3);document.querySelector('#visual-out').onclick=()=>graph?.zoom(1/1.3);
  visualExpand(()=>graph?.fit());
  document.querySelector('#overview').addEventListener('toggle',e=>{if(e.target.open&&!data)load();else if(e.target.open)graph?.fit();});
}
