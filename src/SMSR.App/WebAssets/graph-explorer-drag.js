export function bounded(position,bounds) {
  return {x:Math.max(8,Math.min(bounds.width-208,position.x)),
    y:Math.max(8,Math.min(bounds.height-bounds.nodeHeight-8,position.y))};
}

export function dragNode(group,svg,origin,bounds,move,commit) {
  let current={...origin},active=null,suppressUntil=0;
  Object.assign(group.style,{cursor:'grab',touchAction:'none',userSelect:'none'});
  group.setAttribute('aria-keyshortcuts','ArrowUp ArrowDown ArrowLeft ArrowRight');
  const point=event=>{
    const matrix=svg.getScreenCTM();if(!matrix)return null;
    const value=svg.createSVGPoint();value.x=event.clientX;value.y=event.clientY;
    return value.matrixTransform(matrix.inverse());
  };
  const draw=value=>{current=bounded(value,bounds);group.style.animation='none';move(current)};
  const handlers={
    pointerdown(event){
      if(active||event.button!==0||event.isPrimary===false)return;
      const start=point(event);if(!start)return;
      suppressUntil=0;active={id:event.pointerId,start,startClientX:event.clientX,startClientY:event.clientY,before:{...current},moved:false};
      group.focus({preventScroll:true});group.style.cursor='grabbing';
      try{group.setPointerCapture(event.pointerId)}catch{active=null;group.style.cursor='grab'}
    },
    pointermove(event){
      if(!active||event.pointerId!==active.id)return;
      const value=point(event);if(!value)return;
      const dx=value.x-active.start.x,dy=value.y-active.start.y;
      active.moved ||= Math.hypot(event.clientX-active.startClientX,event.clientY-active.startClientY)>3;
      if(!active.moved)return;
      event.preventDefault();draw({x:active.before.x+dx,y:active.before.y+dy});
    },
    pointerup(event){finish(event,false)},
    pointercancel(event){finish(event,true)},
    lostpointercapture(event){finish(event,true)},
    keydown(event){
      const shift={ArrowLeft:[-1,0],ArrowRight:[1,0],ArrowUp:[0,-1],ArrowDown:[0,1]}[event.key];
      if(!shift||active)return;
      event.preventDefault();const step=event.shiftKey?30:10;
      draw({x:current.x+shift[0]*step,y:current.y+shift[1]*step});commit(current);
    }
  };
  function finish(event,cancel) {
    if(!active||event.pointerId!==active.id)return;
    const before=active;active=null;group.style.cursor='grab';
    if(before.moved){suppressUntil=event.timeStamp+500;if(cancel)draw(before.before);else commit(current)}
    try{if(group.hasPointerCapture(event.pointerId))group.releasePointerCapture(event.pointerId)}catch{}
  }
  const click=event=>{
    if(event.detail!==0&&event.timeStamp<=suppressUntil){suppressUntil=0;event.preventDefault();event.stopImmediatePropagation()}
  };
  for(const [name,handler] of Object.entries(handlers))group.addEventListener(name,handler);
  group.addEventListener('click',click,true);
  return ()=>{
    if(active)finish({pointerId:active.id,timeStamp:0},true);
    for(const [name,handler] of Object.entries(handlers))group.removeEventListener(name,handler);
    group.removeEventListener('click',click,true);
  };
}
