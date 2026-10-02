// Adapted from Graphify 0.9.73 exporters/html.py (Apache-2.0). See Graphify-visualization-NOTICE.txt.
const colors=['#4E79A7','#F28E2B','#E15759','#76B7B2','#59A14F','#EDC948','#B07AA1','#FF9DA7','#9C755F','#BAB0AC'];
export const groupColor=id=>colors[((id%colors.length)+colors.length)%colors.length];
export function visualData(data){
  const meta=data.mode==='communities',max=Math.max(1,...data.nodes.map(n=>meta?n.members:n.degree));
  const nodes=data.nodes.map((n,i)=>{const color=groupColor(n.groupId),weight=meta?n.members:n.degree;
    return {id:n.id,label:n.label,title:`${n.label}\n${n.groupName}${meta?` · ${n.members}개 항목`:''}`,
      size:10+30*weight/max,font:{size:(data.nodes.length<=60||weight>=max*.15)?12:0,color:'#dce9f9'},
      color:{background:color,border:color,highlight:{background:'#ffffff',border:color}},
      x:30*Math.sqrt(i)*Math.cos(i*2.4),y:30*Math.sqrt(i)*Math.sin(i*2.4),group:n.groupId};});
  const edges=data.edges.map(e=>({id:e.id,from:e.sourceId,to:e.targetId,
    title:`${e.relation} [${e.confidence}] · ${e.count}개`,dashes:e.confidence!=='EXTRACTED',
    width:e.confidence==='EXTRACTED'?2:1,color:{color:'#849bb8',opacity:e.confidence==='EXTRACTED'?.7:.35},
    arrows:{to:{enabled:true,scaleFactor:.5}}}));
  return {nodes,edges};
}
