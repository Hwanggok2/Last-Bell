// Validate the small offline tab controller with a DOM stub, not browser QA.
const fs=require('fs'),vm=require('vm'),path=require('path'),assert=require('assert');
const html=fs.readFileSync(path.join(__dirname,'index.html'),'utf8');
const names=['combat','lobby','upgrade','relic','story','wireframe'];
const panels=Object.fromEntries(names.map((x,i)=>['panel-'+x,{hidden:i!==0}]));
const tabs=names.map((name,i)=>({dataset:{tab:name},tabIndex:i? -1:0,attrs:{'aria-selected':String(i===0)},handlers:{},setAttribute(k,v){this.attrs[k]=v},addEventListener(k,fn){this.handlers[k]=fn},focus(){this.focused=true}}));
vm.runInNewContext(html.match(/<script>([\s\S]*?)<\/script>/)[1],{document:{querySelectorAll:()=>tabs,getElementById:id=>panels[id]}});
for(let i=0;i<tabs.length;i++){tabs[i].handlers.click();assert.equal(tabs[i].attrs['aria-selected'],'true');assert.equal(Object.values(panels).filter(x=>!x.hidden).length,1);assert.equal(panels['panel-'+names[i]].hidden,false);}
tabs[5].handlers.keydown({key:'ArrowRight',preventDefault(){}});assert.equal(tabs[0].attrs['aria-selected'],'true');
tabs[0].handlers.keydown({key:'End',preventDefault(){}});assert.equal(tabs[5].attrs['aria-selected'],'true');
console.log('Tab controller: six selections, arrow wrap and End key passed (DOM stub).');
