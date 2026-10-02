export function network(data) {
  const nodes=new Map([[data.node.nodeId,data.node]]), edges=[];
  for(const item of [...(data.incoming||[]),...(data.outgoing||[])]) {
    nodes.set(item.node.nodeId,item.node); edges.push(item.edge);
  }
  for(const node of data.nodes||[])nodes.set(node.nodeId,node);
  edges.push(...(data.edges||[]));
  const groups=new Map();
  for(const edge of edges) {
    if(!nodes.has(edge.sourceId)||!nodes.has(edge.targetId))continue;
    const key=JSON.stringify([edge.sourceId,edge.targetId,edge.relation,edge.confidence,edge.resolution]);
    if(!groups.has(key))groups.set(key,[]);
    const items=groups.get(key);
    if(!items.some(e=>JSON.stringify(e)===JSON.stringify(edge)))items.push(edge);
  }
  const columns=new Map([[data.node.nodeId,0]]), links=[...groups.values()];
  let frontier=[data.node.nodeId];
  for(let level=1;level<=3;level++) {
    const next=[];
    for(const id of frontier)for(const [edge] of links) {
      const other=edge.sourceId===id?edge.targetId:edge.targetId===id?edge.sourceId:null;
      if(!other||columns.has(other))continue;
      const parent=columns.get(id), sign=parent===0?(edge.sourceId===id?1:-1):Math.sign(parent);
      columns.set(other,sign*level);next.push(other);
    }
    frontier=next;
  }
  return {nodes:[...nodes.values()].filter(n=>columns.has(n.nodeId)),links,columns};
}

export function positions(data,heightOf) {
  const map=network(data), columns=[...new Set(map.columns.values())].sort((a,b)=>a-b);
  const grouped=columns.map(column=>map.nodes.filter(n=>map.columns.get(n.nodeId)===column));
  const height=Math.max(450,...grouped.map(ns=>ns.reduce((sum,n)=>sum+heightOf(n)+18,40)));
  const coords=new Map();
  grouped.forEach((nodes,index)=>{
    let y=(height-nodes.reduce((sum,n)=>sum+heightOf(n)+18,0))/2;
    for(const node of nodes){coords.set(node.nodeId,{x:20+index*320,y,node});y+=heightOf(node)+18;}
  });
  return {...map,coords,height,width:Math.max(240,columns.length*320+20)};
}
