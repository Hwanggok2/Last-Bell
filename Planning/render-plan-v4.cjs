const fs=require('node:fs');
const path=require('node:path');
const {chromium}=require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const assert=require('node:assert/strict');
(async()=>{
 const root=__dirname;
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 const page=await browser.newPage({viewport:{width:1440,height:1000},deviceScaleFactor:1});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 const report={date:'2026-10-21',version:'planning-v4',scope:'Document/interactive-wireframe browser checks, not Unity/device/payment tests',screens:[],errors};
 for(const source of ['wireframes-v4.svg','landscape-battle-v4.svg','lobby-v4.svg','relics-v4.svg','relic-choice-v4.svg','exchange-v4.svg']){
  const svg=fs.readFileSync(path.join(root,'Visuals',source),'utf8'),bounds=svg.match(/viewBox="([\d.\s]+)"/)[1].trim().split(/\s+/).map(Number);
  await page.setViewportSize({width:Math.ceil(bounds[2]),height:Math.ceil(bounds[3])});
  await page.setContent('<!doctype html><html><head><meta charset="utf-8"></head><body style="margin:0">'+svg+'</body></html>');
  await page.screenshot({path:path.join(root,'Visuals',source.replace('.svg','.png')),animations:'disabled'});
 }
 await page.setViewportSize({width:1440,height:1000});
 await page.goto('file:///'+path.join(root,'index.html').replaceAll('\\','/'));
 await page.screenshot({path:path.join(root,'Visuals','plan-overview-v4.png')});
 for(const width of [1440,768,360]){
  await page.setViewportSize({width,height:900});
  const r=await page.evaluate(()=>({viewport:innerWidth,documentWidth:document.documentElement.scrollWidth,missingImages:[...document.images].filter(i=>!i.complete||!i.naturalWidth).map(i=>i.src)}));
  report.screens.push(r);
 }
 await page.goto('file:///'+path.join(root,'Visuals','wireframes-v4.html').replaceAll('\\','/'));
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
      box.innerHTML=renderV4(kind,{width,height,left:44,right:32,top:16,bottom:16,state,large},model).svg;
      for(const t of box.querySelectorAll('text')){
       if(!t.textContent.trim())continue;
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
 fs.writeFileSync(path.join(root,'Evidence','wireframe-text-check-v4.json'),JSON.stringify(report.textBounds,null,2));
 await page.setViewportSize({width:1440,height:1000});
 const snapshot=()=>page.evaluate(()=>({kind:app.kind,state:app.state,character:app.character,gold:app.gold,fragments:app.fragments,records:app.records,inventory:app.inventory,pendingOffer:app.pendingOffer,pendingExchange:app.pendingExchange}));
 const act=async id=>{await page.locator(`#live [data-action="${id}"]`).click();};
 await page.locator('[data-scenario="reset"]').click();
 const first=await snapshot();assert.equal(first.kind,'tutorial');
 await act('dodge-up');
 await page.waitForFunction(()=>app.kind==='tutorial'&&app.state==='attack');
 await act('attack');
 await page.waitForFunction(()=>app.kind==='battle');
 const afterTutorial=await snapshot();
 await page.evaluate(()=>scenario('death9'));
 await page.locator('[data-scenario="funds"]').click();
 await act('character-dam');
 const beforeUpgrade=await snapshot();
 await act('upgrade-0');
 const afterUpgrade=await snapshot();
 assert.equal(afterUpgrade.records.dam.levels[0],beforeUpgrade.records.dam.levels[0]+1);
 assert.equal(afterUpgrade.gold,beforeUpgrade.gold-10);
 assert.equal(afterUpgrade.records.on.levels[0],0);
 await page.evaluate(()=>scenario('level9'));
 const beforeBreak=await snapshot();
 await act('upgrade-0');const level10=await snapshot();
 await act('upgrade-0');const level11=await snapshot();
 assert.equal(level10.records.dam.levels[0],10);assert.equal(level11.records.dam.levels[0],11);
 assert.equal(level10.gold,beforeBreak.gold-10);assert.equal(level11.gold,level10.gold-60);
 report.interactions={firstRun:first.kind,tutorialCompletes:afterTutorial.kind==='battle',characterGrowthIndependent:true,costs9to10to11:[10,60]};
 const run=async name=>{await page.evaluate(name=>scenario(name),name);};
 await run('convert');
 const quote=await snapshot();assert.equal(quote.pendingExchange.fragments,2);assert.equal(quote.pendingExchange.gold,200);
 await act('close');const cancelled=await snapshot();
 assert.equal(cancelled.gold,quote.gold);assert.equal(cancelled.fragments,quote.fragments);assert.equal(cancelled.records.dam.levels[0],40);
 await run('convert');await act('convert-confirm');
 const converted=await snapshot();assert.equal(converted.gold,200);assert.equal(converted.fragments,1);assert.equal(converted.records.dam.levels[0],40);
 await page.evaluate(()=>action('convert-confirm'));assert.deepEqual(await snapshot(),converted);
 await act('upgrade-0');const upgraded=await snapshot();assert.equal(upgraded.gold,50);assert.equal(upgraded.records.dam.levels[0],41);
 await run('fragment-shortage');const shortageBefore=await snapshot();
 // Disabled confirmation and command guard must both prevent a debit.
 await page.evaluate(()=>action('convert-confirm'));const shortageAfter=await snapshot();
 assert.equal(shortageAfter.gold,shortageBefore.gold);assert.equal(shortageAfter.fragments,shortageBefore.fragments);
 await run('convert');await page.evaluate(()=>{app.gold=50;action('convert-confirm');});
 const stale=await snapshot();assert.equal(stale.gold,50);assert.equal(stale.fragments,3);assert.equal(stale.records.dam.levels[0],40);
 report.interactions.exchange={quote:[2,200],cancelNoDebit:true,confirmDoesNotUpgrade:true,duplicateConfirmNoDebit:true,manualUpgradeRemainder:50,insufficientNoDebit:true,staleQuoteNoDebit:true};
 await run('reset');await run('finish5');
 let offer=await snapshot();assert.equal(offer.kind,'reward');assert.equal(new Set(offer.pendingOffer.ids).size,3);
 const firstOffer=offer.pendingOffer,firstId=firstOffer.ids[0],fragmentsBefore=offer.fragments;
 await page.reload();offer=await snapshot();assert.deepEqual(offer.pendingOffer,firstOffer);assert.equal(offer.fragments,fragmentsBefore);
 await act('reward-'+firstId);const afterPick=await snapshot();assert.equal(afterPick.kind,'battle');assert.equal(afterPick.records.on.stage,6);assert.equal(afterPick.inventory[firstId],1);
 await page.evaluate(id=>action('reward-'+id),firstId);assert.deepEqual(await snapshot(),afterPick);
 await run('finish10');offer=await snapshot();assert.equal(offer.kind,'reward');assert.equal(offer.records.on.checkpoint,1);
 await act('reward-'+offer.pendingOffer.ids[0]);const eleven=await snapshot();assert.equal(eleven.kind,'battle');assert.equal(eleven.records.on.stage,11);assert.equal(eleven.records.on.checkpoint,11);
 await run('kill');const twelve=await snapshot();assert.equal(twelve.kind,'battle');assert.equal(twelve.records.on.stage,12);
 await run('death');assert.equal((await page.locator('#live [data-id="start"]').textContent()).trim(),'Stage 11');
 await run('duplicate');await act('reward-relic_05');const duplicate=await snapshot();assert.equal(duplicate.inventory.relic_05,2);assert.equal(duplicate.records.on.stage,6);
 await run('max');const beforeMax=await snapshot();await act('reward-relic_05');const afterMax=await snapshot();assert.equal(afterMax.inventory.relic_05,5);assert.equal(afterMax.gold-beforeMax.gold,20);
 await run('ending');offer=await snapshot();assert.equal(offer.kind,'reward');await act('reward-'+offer.pendingOffer.ids[0]);assert.equal((await snapshot()).kind,'ending');await act('finish50');
 const end=await snapshot();assert.equal(end.kind,'lobby');assert.equal(end.records.on.ng,1);assert.equal(end.records.on.checkpoint,1);assert.equal(end.records.rin.ng,0);
 await page.evaluate(()=>action('finish50'));assert.deepEqual(await snapshot(),end);
 report.interactions.relics={threeDistinctCandidates:true,restoreSameOffer:true,newLevel1:true,duplicateLevel2:true,maxLevel5Gold20AtStage5:true,duplicateSelectionRejected:true,bossChoiceThenAuto11:true,normalAuto12:true,stageLabel:'Stage 11',endingAfterChoice:true,cycleOnce:true};
 await run('relics-owned');await act('relic-equip');let gear=await snapshot();assert(gear.records.on.equipped.includes('relic_05'));
 await act('relic-unequip');gear=await snapshot();assert(!gear.records.on.equipped.includes('relic_05'));
 report.interactions.relics.equipAndUnequip=true;
 await page.screenshot({path:path.join(root,'Visuals','wireframes-preview-v4.png'),fullPage:false});
 for(const [kind,state,width,height,file] of [['lobby','normal',640,360,'lobby-small-v4.png'],['battle','rear',640,360,'battle-small-v4.png'],['battle','hit',960,540,'damage-normal-v4.png'],['battle','critical',960,540,'damage-critical-v4.png'],['battle','mixed',640,360,'damage-mixed-small-v4.png'],['relics','normal',640,360,'relics-small-v4.png']]){
  const svg=await page.evaluate(({kind,state,width,height})=>renderV4(kind,{width,height,left:44,right:32,top:16,bottom:16,state,large:true,color:true},model).svg,{kind,state,width,height});
  await page.setViewportSize({width,height});
  await page.setContent('<!doctype html><html><head><meta charset="utf-8"></head><body style="margin:0">'+svg+'</body></html>');
  await page.screenshot({path:path.join(root,'Visuals',file),animations:'disabled'});
 }
 fs.writeFileSync(path.join(root,'Evidence','document-render-check-v4.json'),JSON.stringify(report,null,2));
 await browser.close();
 assert.equal(errors.length,0);
 console.log(JSON.stringify({...report,textBounds:{cases:report.textBounds.cases,texts:report.textBounds.texts,issueCount:report.textBounds.issues.length,firstIssues:report.textBounds.issues.slice(0,8)}}));
 for(const screen of report.screens){assert.equal(screen.documentWidth,screen.viewport);assert.equal(screen.missingImages.length,0);}
 if(report.textBounds.issues.length)process.exitCode=1;
})().catch(e=>{console.error(e);process.exit(1)});
