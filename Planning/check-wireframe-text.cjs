const fs = require('node:fs');
const path = require('node:path');
const {chromium} = require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async()=>{
  const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  const page=await browser.newPage({viewport:{width:1440,height:900}});
  await page.goto('file:///'+path.join(__dirname,'Visuals','wireframes-v2.html').replaceAll('\\','/'));
  const result=await page.evaluate(()=>{
    const box=document.createElement('div');box.style.cssText='position:absolute;left:0;top:0;background:white';document.body.append(box);
    let cases=0,texts=0;const issues=[];
    for(const [width,height] of [[640,360],[960,540],[1200,540]]){
      for(const large of [false,true]){
        for(const [kind,variants] of Object.entries(states)){
          for(const [state] of variants){
            cases++;
            box.innerHTML=renderV2(kind,{width,height,left:44,right:32,top:16,bottom:16,state,large,controls:'three'},model).svg;
            for(const t of box.querySelectorAll('text')){
              const owner=t.closest('[data-bounds]');
              if(!owner)continue;
              texts++;
              const [x,y,w,h]=owner.dataset.bounds.split(',').map(Number),b=t.getBBox();
              if(b.x<x-2||b.y<y-2||b.x+b.width>x+w+2||b.y+b.height>y+h+2){
                issues.push({width,height,large,kind,state,id:owner.dataset.id,text:t.textContent,bounds:[x,y,w,h],actual:[b.x,b.y,b.width,b.height]});
              }
            }
          }
        }
      }
    }
    box.remove();return {cases,texts,issues};
  });
  fs.writeFileSync(path.join(__dirname,'Evidence','wireframe-text-check.json'),JSON.stringify({date:'2026-10-21',scope:'Browser SVG text containment, not physical-device readability',...result},null,2));
  await browser.close();
  console.log(JSON.stringify({cases:result.cases,texts:result.texts,issueCount:result.issues.length,firstIssues:result.issues.slice(0,12)}));
})().catch(e=>{console.error(e);process.exit(1)});
