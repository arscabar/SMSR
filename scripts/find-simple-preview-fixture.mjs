// Minimal DOM substitute: tests logic only, not browser layout or accessibility.
export function previewDocument() {
  const nodes=new Map();
  function element(tag){return {tag,children:[],dataset:{},attributes:{},textContent:'',className:'',value:'',hidden:false,open:false,
    append(...children){this.children.push(...children);},
    replaceChildren(...children){this.children=[...children];},
    setAttribute(k,v){this.attributes[k]=v;},
    addEventListener(k,f){this['on'+k]=f;},
    closest(){return this;},showModal(){this.open=true;}
  };}
  for(const id of ['selection','results','count','query','search','filters','source-title','source-text','source','graph','connections'])nodes.set(id,element('div'));
  for(const kind of ['', 'code','document','image','video','audio']){const b=element('button');b.dataset.kind=kind;nodes.get('filters').append(b);}
  return {nodes,createElement:element,querySelector:s=>nodes.get(s.slice(1)),querySelectorAll:s=>{
    if(s==='.result')return nodes.get('results').children.filter(n=>n.className.startsWith('result'));
    if(s==='#filters button')return nodes.get('filters').children;
    throw new Error('Unsupported test selector: '+s);
  }};
}
