const toolbars=new WeakMap();
export function viewport(graph,svg,canvas){
  let controls=toolbars.get(graph);
  if(!controls){
    const box=document.createElement('span');box.setAttribute('aria-label','지도 배율');
    Object.assign(box.style,{display:'inline-flex',gap:'4px',alignItems:'center'});
    controls={};
    for(const [key,label] of [['out','축소'],['in','확대'],['fit','전체 보기']]){
      const button=document.createElement('button');button.type='button';button.textContent=label;
      button.setAttribute('aria-label','지도 '+label);controls[key]=button;box.append(button);
    }
    controls.label=document.createElement('output');controls.label.setAttribute('aria-live','polite');box.append(controls.label);
    document.querySelector('#graph-tools').append(box);toolbars.set(graph,controls);
  }
  const fitScale=()=>Math.min((graph.clientWidth||canvas.width)/canvas.width,(graph.clientHeight||canvas.height)/canvas.height);
  let fit=fitScale(),scale=fit,fitted=true;
  function apply(next){
    if(fitted)fit=fitScale();
    const center=fitted?{x:canvas.width/2,y:canvas.height/2}:
      {x:(graph.scrollLeft+graph.clientWidth/2)/scale,y:(graph.scrollTop+graph.clientHeight/2)/scale};
    fitted=next===null||next<=fit+1e-9;scale=fitted?fit:Math.min(next,Math.max(fit,3));
    Object.assign(svg.style,{width:fitted?'100%':`${canvas.width*scale}px`,height:fitted?'100%':`${canvas.height*scale}px`,minWidth:'0'});
    if(fitted){fit=fitScale();scale=fit;}
    graph.scrollLeft=fitted?0:Math.max(0,center.x*scale-graph.clientWidth/2);
    graph.scrollTop=fitted?0:Math.max(0,center.y*scale-graph.clientHeight/2);
    controls.out.disabled=fitted;controls.in.disabled=scale>=Math.max(fit,3);
    controls.label.textContent=`${fitted?'전체 · ':''}${Math.round(scale*100)}%`;
  }
  controls.in.onclick=()=>apply((fitted?fitScale():scale)*1.5);
  controls.out.onclick=()=>apply(scale/1.5);controls.fit.onclick=()=>apply(null);apply(null);
  return ()=>{for(const key of ['in','out','fit']){controls[key].onclick=null;controls[key].disabled=true;}};
}
