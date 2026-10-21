const fs=require('node:fs');
const path=require('node:path');
const {chromium}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert=require('node:assert/strict');
(async()=>{
 const root=__dirname;
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 const page=await browser.newPage({viewport:{width:1440,height:1000},deviceScaleFactor:1});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const report={date:'2026-10-21',version:'planning-v3',scope:'Document/interactive-wireframe browser checks, not Unity/device/payment tests',screens:[],errors};
 await page.goto('file:///'+path.join(root,'index.html').replaceAll('\\','/'));
 await page.screenshot({path:path.join(root,'Visuals','plan-overview-v3.png')});
 for(const width of [1440,768,360]){
  await page.setViewportSize({width,height:900});
  const r=await page.evaluate(()=>({viewport:innerWidth,documentWidth:document.documentElement.scrollWidth,missingImages:[...document.images].filter(i=>!i.complete||!i.naturalWidth).map(i=>i.src)}));
  report.screens.push(r);
 }
 await page.goto('file:///'+path.join(root,'Visuals','wireframes-v3.html').replaceAll('\\','/'));
 report.wireframeScreens=[];
 for(const width of [1600,1440,768,360]){
  await page.setViewportSize({width,height:900});
  const r=await page.evaluate(()=>({viewport:innerWidth,documentWidth:document.documentElement.scrollWidth}));
  report.wireframeScreens.push(r);assert.equal(r.documentWidth,width);
 }
 report.textBounds=await page.evaluate(()=>{
  const box=document.createElement('div');box.style.cssText='position:absolute;left:0;top:0;background:white';document.body.append(box);
  let cases=0,texts=0;const issues=[];
  for(const [width,height] of [[640,360],[960,540],[1200,540]]){
   for(const large of [false,true]){
    for(const [kind,variants] of Object.entries(states)){
     for(const [state] of variants){
      cases++;
      box.innerHTML=renderV3(kind,{width,height,left:44,right:32,top:16,bottom:16,state,large},model).svg;
      for(const t of box.querySelectorAll('text')){
       const owner=t.closest('[data-bounds]');if(!owner)continue;
       texts++;const [x,y,w,h]=owner.dataset.bounds.split(',').map(Number),b=t.getBBox();
       if(b.x<x-2||b.y<y-2||b.x+b.width>x+w+2||b.y+b.height>y+h+2)issues.push({width,height,large,kind,state,id:owner.dataset.id,text:t.textContent,bounds:[x,y,w,h],actual:[b.x,b.y,b.width,b.height]});
      }
     }
    }
   }
  }
  box.remove();return{cases,texts,issues};
 });
 fs.writeFileSync(path.join(root,'Evidence','wireframe-text-check-v3.json'),JSON.stringify(report.textBounds,null,2));
 await page.setViewportSize({width:1440,height:1000});
 const snapshot=()=>page.evaluate(()=>({kind:app.kind,state:app.state,character:app.character,gold:app.gold,fragments:app.fragments,records:app.records}));
 const act=async id=>{await page.locator(`#live [data-action="${id}"]`).click();};
 await page.locator('[data-scenario="reset"]').click();
 const first=await snapshot();assert.equal(first.kind,'tutorial');
 await act('dodge-up');
 await page.waitForFunction(()=>app.kind==='tutorial'&&app.state==='attack');
 await act('attack');
 await page.waitForFunction(()=>app.kind==='battle');
 const afterTutorial=await snapshot();
 await page.locator('[data-scenario="death9"]').click();
 await page.locator('[data-scenario="funds"]').click();
 await act('character-dam');
 const beforeUpgrade=await snapshot();
 await act('upgrade-0');
 const afterUpgrade=await snapshot();
 assert.equal(afterUpgrade.records.dam.levels[0],beforeUpgrade.records.dam.levels[0]+1);
 assert.equal(afterUpgrade.gold,beforeUpgrade.gold-10);
 assert.equal(afterUpgrade.records.on.levels[0],0);
 await page.locator('[data-scenario="level9"]').click();
 const beforeBreak=await snapshot();
 await act('upgrade-0');const level10=await snapshot();
 await act('upgrade-0');const level11=await snapshot();
 assert.equal(level10.records.dam.levels[0],10);assert.equal(level11.records.dam.levels[0],11);
 assert.equal(level10.gold,beforeBreak.gold-10);assert.equal(level11.gold,level10.gold-60);
 report.interactions={firstRun:first.kind,tutorialCompletes:afterTutorial.kind==='battle',characterGrowthIndependent:true,costs9to10to11:[10,60]};
 await page.screenshot({path:path.join(root,'Visuals','wireframes-preview-v3.png'),fullPage:false});
 for(const [kind,state,file] of [['lobby','normal','lobby-small-v3.png'],['battle','rear','battle-small-v3.png']]){
  const svg=await page.evaluate(({kind,state})=>renderV3(kind,{width:640,height:360,left:44,right:32,top:16,bottom:16,state,large:true,color:true},model).svg,{kind,state});
  await page.setViewportSize({width:640,height:360});
  await page.setContent('<!doctype html><html><head><meta charset="utf-8"></head><body style="margin:0">'+svg+'</body></html>');
  await page.screenshot({path:path.join(root,'Visuals',file),animations:'disabled'});
 }
 for(const source of ['wireframes-v3.svg','landscape-battle-v3.svg','lobby-v3.svg']){
  const svg=fs.readFileSync(path.join(root,'Visuals',source),'utf8'),bounds=svg.match(/viewBox="([\d.\s]+)"/)[1].trim().split(/\s+/).map(Number);
  await page.setViewportSize({width:Math.ceil(bounds[2]),height:Math.ceil(bounds[3])});
  await page.setContent('<!doctype html><html><head><meta charset="utf-8"></head><body style="margin:0">'+svg+'</body></html>');
  await page.screenshot({path:path.join(root,'Visuals',source.replace('.svg','.png')),animations:'disabled'});
 }
 fs.writeFileSync(path.join(root,'Evidence','document-render-check-v3.json'),JSON.stringify(report,null,2));
 await browser.close();
 assert.equal(errors.length,0);
 console.log(JSON.stringify({...report,textBounds:{cases:report.textBounds.cases,texts:report.textBounds.texts,issueCount:report.textBounds.issues.length,firstIssues:report.textBounds.issues.slice(0,8)}}));
 for(const screen of report.screens){assert.equal(screen.documentWidth,screen.viewport);assert.equal(screen.missingImages.length,0);}
 if(report.textBounds.issues.length)process.exitCode=1;
})().catch(e=>{console.error(e);process.exit(1)});
