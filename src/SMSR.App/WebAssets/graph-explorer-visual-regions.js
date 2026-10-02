// Graphify exporters/html.py _hyperedge_script: shaded convex hulls, Apache-2.0.
export function convexHull(points){
  const p=points.slice().sort((a,b)=>a.x-b.x||a.y-b.y);
  if(p.length<3)return p;
  const cross=(o,a,b)=>(a.x-o.x)*(b.y-o.y)-(a.y-o.y)*(b.x-o.x);
  const build=seq=>{const out=[];for(const q of seq){while(out.length>=2&&cross(out.at(-2),out.at(-1),q)<=0)out.pop();out.push(q);}out.pop();return out;};
  const hull=build(p).concat(build(p.slice().reverse()));return hull.length>=3?hull:p;
}
export function hyperRegions(network,nodes,regions){network.on('afterDrawing',ctx=>{
  for(const h of regions){const ids=h.nodes.filter(id=>nodes.get(id)&&!nodes.get(id).hidden);
    if(ids.length!==h.nodes.length||ids.length<2)continue;
    const positions=Object.values(network.getPositions(ids));if(positions.length<2)continue;
    const cx=positions.reduce((s,p)=>s+p.x,0)/positions.length,cy=positions.reduce((s,p)=>s+p.y,0)/positions.length;
    const hull=convexHull(positions).map(p=>({x:cx+(p.x-cx)*1.15,y:cy+(p.y-cy)*1.15}));
    ctx.save();ctx.globalAlpha=.12;ctx.fillStyle='#6366f1';ctx.strokeStyle='#6366f1';ctx.lineWidth=2;ctx.beginPath();
    ctx.moveTo(hull[0].x,hull[0].y);for(const p of hull.slice(1))ctx.lineTo(p.x,p.y);ctx.closePath();ctx.fill();ctx.globalAlpha=.4;ctx.stroke();
    ctx.globalAlpha=.8;ctx.fillStyle='#aeb0ff';ctx.font='bold 11px sans-serif';ctx.textAlign='center';ctx.fillText(h.label,cx,cy-5);ctx.restore();
  }
});}
