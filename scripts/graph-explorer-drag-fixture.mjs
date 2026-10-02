export class Element {
  constructor(tag='g'){this.tag=tag;this.style={};this.attributes=new Map();this.children=[];this.listeners=new Map();this.captured=null}
  setAttribute(key,value){this.attributes.set(key,String(value))} getAttribute(key){return this.attributes.get(key)} removeAttribute(key){this.attributes.delete(key)}
  append(...nodes){this.children.push(...nodes)} replaceChildren(...nodes){this.children=[...nodes]}
  cloneNode(){const node=new Element(this.tag);node.attributes=new Map(this.attributes);node.style={...this.style};return node}
  focus(){this.focused=true}
  setPointerCapture(id){this.captured=id} hasPointerCapture(id){return this.captured===id} releasePointerCapture(){this.captured=null}
  getScreenCTM(){return {inverse:()=>({scale:1})}}
  createSVGPoint(){return {x:0,y:0,matrixTransform(matrix){return {x:this.x*matrix.scale,y:this.y*matrix.scale}}}}
  addEventListener(name,handler,capture=false){const values=this.listeners.get(name)||[];values.push({handler,capture});this.listeners.set(name,values)}
  removeEventListener(name,handler){this.listeners.set(name,(this.listeners.get(name)||[]).filter(value=>value.handler!==handler))}
  event(name,values={}){
    const event={button:0,isPrimary:true,pointerId:1,timeStamp:100,detail:1,preventDefault(){this.prevented=true},stopImmediatePropagation(){this.stopped=true},...values};
    for(const {handler} of [...(this.listeners.get(name)||[])].sort((a,b)=>Number(b.capture)-Number(a.capture))){handler(event);if(event.stopped)break}
    if(!event.stopped&&!this.disabled)this['on'+name]?.(event);return event;
  }
}
