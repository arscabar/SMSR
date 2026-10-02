import {kindClass,kindName,labelLines} from './graph-explorer-labels.js';
const location=node=>node.sourcePath||node.ownerPath;
const locationLines=node=>node.kind==='symbol'&&location(node)?labelLines(`${location(node)}${node.line?':'+node.line:''}`,36):[];
export const heightOf=node=>38+labelLines(node.label).length*16+locationLines(node).length*12;
export function svgElement(tag,attributes={}) {
  const node=document.createElementNS('http://www.w3.org/2000/svg',tag);
  for(const [key,value] of Object.entries(attributes))node.setAttribute(key,value);
  return node;
}
export function button(node,label,action) {
  node.setAttribute('aria-label',label);node.setAttribute('tabindex','0');node.setAttribute('role','button');
  node.style.cursor='pointer';node.addEventListener('click',action);
  node.addEventListener('keydown',event=>{if(event.key==='Enter'||event.key===' '){event.preventDefault();action()}});
}
export function svgNode(svg,node,x,y,selected,action) {
  const group=svgElement('g',{'data-node-id':node.nodeId,class:`graph-node ${kindClass(node.kind)}${selected?' selected':''}`});
  const description=[node.label,location(node)&&`${location(node)}${node.line?':'+node.line:''}`].filter(Boolean).join(' · ');
  button(group,description,action);
  const rect=svgElement('rect',{x,y,width:200,height:heightOf(node),rx:10});
  if(selected)rect.style.strokeWidth='4';
  const tooltip=svgElement('title');tooltip.textContent=description;
  const title=svgElement('text',{x:x+10});
  labelLines(node.label).forEach((line,index)=>{
    const part=svgElement('tspan',{x:x+10,y:y+22+index*16});part.textContent=line;title.append(part);
  });
  const source=svgElement('text',{class:'edge-label'});
  locationLines(node).forEach((line,index)=>{
    const part=svgElement('tspan',{x:x+10,y:y+24+labelLines(node.label).length*16+index*12});part.textContent=line;source.append(part);
  });
  const kind=svgElement('text',{x:x+10,y:y+heightOf(node)-12,class:'edge-label'});kind.textContent=kindName(node.kind);
  group.append(tooltip,rect,title,source,kind);svg.append(group);
  return group;
}
