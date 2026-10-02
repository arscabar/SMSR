export function visualExpand(fit){
  const panel=document.querySelector('#overview'),button=document.querySelector('#visual-expand');
  const toggle=()=>{const active=panel.classList.toggle('visual-expanded');button.textContent=active?'크게 보기 닫기':'크게 보기';button.setAttribute('aria-pressed',String(active));requestAnimationFrame(fit);};
  button.addEventListener('click',toggle);
  document.addEventListener('keydown',e=>{if(e.key==='Escape'&&panel.classList.contains('visual-expanded')){toggle();button.focus();}});
}
