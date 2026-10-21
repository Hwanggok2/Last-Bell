const fs=require('node:fs');
const path=require('node:path');
const assert=require('node:assert/strict');
const root=__dirname;
const d=JSON.parse(fs.readFileSync(path.join(root,'decisions.json'),'utf8'));
assert.equal(d.version,'planning-v2');
const c=d.combat,g=d.growth,e=d.economy;
assert.equal(c.dodgeActiveMs+c.dodgeRecoveryMs,c.dodgeReuseMs);
const worstInputSpacing=c.comboMinGapMs-c.dodgeActiveMs;
assert(worstInputSpacing>=c.dodgeReuseMs);
assert(c.perfectWindowMs<c.dodgeActiveMs);
assert.equal(c.normalStagger,20);assert.equal(c.perfectStagger,40);
const kind=s=>s%10===0?'boss':s%10===5?'miniboss':'normal';
const cost=L=>L%10===0?e.costPerStat.milestone[L/10-1]:e.costPerStat.regular[Math.floor(L/10)];
const totalCost=L=>Array.from({length:L-1},(_,i)=>cost(i+2)).reduce((a,b)=>a+b,0)*2;
const gold=s=>e.normalGoldPerZone[Math.floor((s-1)/10)]*(kind(s)==='boss'?e.bossGoldMultiplier:kind(s)==='miniboss'?e.minibossGoldMultiplier:1);
let wallet=e.initialGold;const affordability=[];
for(let s=1;s<=30;s++){
  assert(wallet>=totalCost(s),`Both stats at level ${s} not affordable from first-clear gold before stage ${s}`);
  if(s%10===0)affordability.push({beforeStage:s,earnedGold:wallet,bothStatCost:totalCost(s),remaining:wallet-totalCost(s)});
  wallet+=gold(s);
}
assert.deepEqual(affordability.map(x=>x.earnedGold),[440,1410,2860]);
assert.equal(totalCost(30),2840);
const gemTotal=d.relics.dropStages.length*e.firstBossGems;
assert.equal(gemTotal,90);assert.equal(e.slotCosts.reduce((a,b)=>a+b),60);
assert.equal(2*e.firstBossGems-e.slotCosts[0],10);
assert.equal(4*e.firstBossGems-e.slotCosts[0]-e.slotCosts[1],0);
const replacementBossClears=Math.ceil(e.exchangeGems/e.repeatBossGems);
assert.equal(replacementBossClears,3);
assert.deepEqual(d.content.minibossStages,[5,15,25]);assert.deepEqual(d.content.bossStages,[10,20,30]);
assert.equal(Array.from({length:30},(_,i)=>kind(i+1)).filter(x=>x==='normal').length,24);
assert.equal(d.relics.definitions.length,6);assert.equal(new Set(d.relics.definitions.map(x=>x.id)).size,6);
const normalBoost=d.relics.definitions.find(x=>x.effect==='normalStaggerPercent').value;
const perfectBoost=d.relics.definitions.find(x=>x.effect==='perfectStaggerPercent').value;
assert.equal(Math.ceil(c.staggerThreshold/(c.normalStagger*(1+normalBoost))),4);
assert.equal(Math.ceil(c.staggerThreshold/(c.perfectStagger*(1+perfectBoost))),2);
assert.equal(c.relicsChangeDodgeTiming,false);
assert.equal(d.resources.animationFrames,14+4*19+3*20);
const b=d.business.baseFirst30Days;
const revenue=b.mau*b.payerRate*b.arppuKRW*(1-b.refundRate)/1.1*(1-b.iapFee)+b.dau*30*b.adImpressionsPerActiveUserDay*b.publisherEcpmKRW/1000;
assert.equal(revenue,130080);
const missing=[];
for(const name of ['index.html','Visuals/wireframes-v2.html']){
 const full=path.join(root,name),html=fs.readFileSync(full,'utf8');
 for(const [,href]of html.matchAll(/(?:href|src)=["']([^"']+)["']/g)){
  if(/^(https?:|data:|#|javascript:)/.test(href))continue;
  const clean=href.split('#')[0].split('?')[0];
  if(clean&&!fs.existsSync(path.resolve(path.dirname(full),clean)))missing.push({file:name,href});
 }
}
assert.deepEqual(missing,[]);
const main=fs.readFileSync(path.join(root,'index.html'),'utf8');
assert(!main.includes('전체 완주 24 장식 조각'));
assert(!main.includes('다시 산정 중'));
const results={date:'2026-10-21',version:d.version,combat:{reuseMs:c.dodgeReuseMs,worstValidInputSpacingMs:worstInputSpacing,slackMs:worstInputSpacing-c.dodgeReuseMs,normalDodgesToStun:Math.ceil(c.staggerThreshold/c.normalStagger),perfectDodgesToStun:Math.ceil(c.staggerThreshold/c.perfectStagger),boostedNormalDodges:4,boostedPerfectDodges:2},economy:{all30StagesLevelMatchedPathAffordable:true,beforeBosses:affordability,allFirstClearGold:wallet,bothStatsMaxCost:totalCost(30),firstClearGems:gemTotal,totalSlotCost:60,bossClearsToReplaceOneExchange:replacementBossClears},animationFrames:d.resources.animationFrames,revenueSensitivity:revenue,missingLocalLinks:missing,limits:'Planning arithmetic and document integrity; no Unity, player, device, purchase or measured campaign test.'};
fs.writeFileSync(path.join(root,'Evidence','planning-verification.json'),JSON.stringify(results,null,2));
console.log(JSON.stringify(results));
