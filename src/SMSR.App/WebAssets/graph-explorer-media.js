export function mediaAnalysis(node,box,{get,projectId}){
 if(!['image','audio','video'].includes(node.kind)||/\.(svg|avif)$/i.test(node.sourcePath))return;
 const panel=document.createElement('details'),summary=document.createElement('summary');summary.textContent='내용 분석 · 영역과 시간 근거';panel.append(summary);box.append(panel);
 const button=document.createElement('button');button.type='button';button.textContent='로컬 분석 확인';panel.append(button);
 const status=document.createElement('p');status.setAttribute('role','status');panel.append(status);let retry=false;
 button.onclick=async()=>{
  button.disabled=true;status.textContent='현재 원문 검증 중…';
  try{const result=await get('/api/graph/media-analysis',{projectId,path:node.sourcePath,retry});retry=false;
   if(result.status==='FAILED'){status.textContent=result.error;button.textContent='분석 재시도';retry=true;return;}
   if(result.status!=='READY'){status.textContent='분석 중입니다. 잠시 후 다시 확인하세요.';return;}
   const data=result.analysis,names={OCR_UNAVAILABLE:'OCR 도구 없음 · 시각 해석 필요',TRANSCRIPTION_UNAVAILABLE:'음성 전사 도구/모델 없음',FRAMES_AVAILABLE:'영상 표본 프레임 준비',TEXT_AVAILABLE:'텍스트 근거 준비',NO_SPEECH:'음성 없음',NO_TEXT:'텍스트 없음'};
   status.textContent=(names[data.status]||data.status)+' · '+data.engine+' · 저장 리비전 '+data.revision;
   for(const old of [...panel.querySelectorAll('.media-analysis-block')])old.remove();
   for(const block of data.blocks.slice(0,30)){
    const link=document.createElement('a');link.className='media-analysis-block';link.textContent=block.location+' · '+block.text;link.style.display='block';
    link.href='/graph/document?'+new URLSearchParams({projectId,path:node.sourcePath,location:block.location});panel.append(link);
   }
   if(data.blocks.length>30)status.textContent+=' · 근거 일부 표시';
  }catch(error){status.textContent=error.message;}finally{button.disabled=false;}
 };
}
