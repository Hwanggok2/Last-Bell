const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const d = JSON.parse(fs.readFileSync(path.join(__dirname,'decisions.json'),'utf8'));
assert.equal(d.version,'planning-v4');
assert.deepEqual(d.input.dodgeDirections,['up','back','down','forward']);
assert.equal(d.visual.attackNameText,false);
assert.equal(d.visual.staggerText,false);
assert.equal(d.visual.lobbyListWidthFraction,.25);
assert.equal(d.combat.dodgeActiveMs+d.combat.dodgeRecoveryMs,d.combat.dodgeReuseMs);
assert(d.combat.comboMinGapMs-d.combat.dodgeActiveMs >= d.combat.dodgeReuseMs);
const g=d.growth,e=d.economy;
const tier=L=>Math.max(0,Math.floor((L-1)/10));
const bellCount=L=>L===0?0:(L-1)%10+1;
const q=S=>g.starScale[S-1];
const attack=(L,S)=>(g.attackBase+g.attackLinearPerLevel*q(S)*L)*(g.attackBreakBase+g.attackBreakPerStar*(S-1))**tier(L);
const hp=(L,S)=>Math.round((g.hpBase+g.hpLinearPerLevel*q(S)*L)*(g.hpBreakBase+g.hpBreakPerStar*(S-1))**tier(L));
assert.equal(g.minLevel,0);assert.equal(g.maxLevel,50);
assert.deepEqual(g.milestones,[11,21,31,41]);
assert.deepEqual([0,9,10,11,20,21,50].map(bellCount),[0,9,10,1,10,1,10]);
const growthCases=[];
const effective=(L,S)=>q(S)*(L+g.effectiveMilestoneLevels*tier(L));
const crit=(L,S)=>Math.min(d.combat.critChanceCap,g.critBase+g.critPerEffectiveLevel*effective(L,S));
const bonus=(L,S)=>Math.min(d.combat.goldLuckBonusCap,g.goldBonusPerEffectiveLevel*effective(L,S));
const resonance=(L,S)=>Math.min(d.combat.stunDamageMultiplierCap,g.resonanceBase+g.resonancePerEffectiveLevel*effective(L,S));
for(let S=1;S<=5;S++){
 for(let L=1;L<=50;L++){
  assert(attack(L,S)>attack(L-1,S));
  assert(hp(L,S)>hp(L-1,S));
  assert(crit(L,S)>crit(L-1,S));assert(bonus(L,S)>bonus(L-1,S));assert(resonance(L,S)>resonance(L-1,S));
  if(S>1){
   assert(attack(L,S)-attack(L-1,S)>attack(L,S-1)-attack(L-1,S-1)-1e-8);
   // Rounded HP can have equal adjacent increments; the unrounded growth is strictly ordered.
   assert(hp(L,S)>=hp(L,S-1));
  }
 }
 for(const L of g.milestones){
  assert(attack(L,S)/attack(L-1,S)>1.9);
  assert(attack(L,S)-attack(L-1,S)>attack(L-1,S)-attack(L-2,S));
 }
 growthCases.push({stars:S,attack10:attack(10,S),attack11:attack(11,S),attack50:attack(50,S),hp50:hp(50,S)});
}
const cost=L=>{
 const milestone=g.milestones.indexOf(L);
 return milestone>=0?e.breakthroughUpgradeCosts[milestone]:e.regularUpgradeCosts[tier(L)];
};
const spent=L=>Array.from({length:L},(_,i)=>cost(i+1)).reduce((a,b)=>a+b,0);
assert.equal(spent(50),1780);
const kind=s=>s%10===0?'boss':s%5===0?'miniboss':'normal';
const gold=s=>e.normalGoldPerZone[Math.floor((s-1)/10)]*e.goldMultipliers[kind(s)];
const fragments=s=>kind(s)==='boss'?e.bossGems:kind(s)==='miniboss'?e.minibossGems:0;
assert.equal(e.minibossGems,1);assert.equal(e.bossGems,3);
let sumGold=0,sumFragments=0;const budgets=[];
for(let s=1;s<=50;s++){
 sumGold+=gold(s);sumFragments+=fragments(s);
 if(s%10===0){
  const target=s===50?50:s+1,totalCost=spent(target)*4;
  assert(sumGold>=totalCost);
  budgets.push({completedStage:s,basicGold:sumGold,fourStatsTarget:target,totalCost,remainder:sumGold-totalCost,fragments:sumFragments});
 }
}
assert.equal(sumGold,8000);assert.equal(sumFragments,20);
assert.equal(e.slotCosts.reduce((a,b)=>a+b),12);
assert.equal(8-e.slotCosts[0],4); // Stage20 after buying slot2: not enough for slot3.
assert.equal(12-e.slotCosts[0],e.slotCosts[1]); // Stage30 affords slot3.
assert.equal(e.repeatKillSameGems,true);
// Executable examples of the documented save contract; this is not game implementation.
const profile=id=>({id,cycle:0,checkpoint:0,highest:0,readyForNext:false});
const commit=(account,char,stage,attempt)=>{
 if(account.committed.has(attempt))return false;
 account.committed.add(attempt);
 account.fragments+=fragments(stage);account.gold+=gold(stage);
 char.highest=Math.max(char.highest,stage);
 if(stage===50)char.readyForNext=true;
 else if(stage%10===0)char.checkpoint=Math.max(char.checkpoint,stage);
 return true;
};
const start=char=>{
 if(char.readyForNext){char.cycle++;char.checkpoint=0;char.readyForNext=false;}
 return char.checkpoint+1;
};
const account={gold:0,fragments:0,committed:new Set()},on=profile('on'),rin=profile('rin');
commit(account,on,9,'on-cycle0-9-a');assert.equal(start(on),1);
commit(account,on,10,'on-cycle0-10-a');assert.equal(start(on),11);
commit(account,on,19,'on-cycle0-19-a');assert.equal(start(on),11);
assert.equal(start(rin),1);assert.equal(rin.cycle,0);
const balance=account.fragments;
assert.equal(commit(account,on,10,'on-cycle0-10-a'),false);
assert.equal(account.fragments,balance);
commit(account,on,15,'on-cycle0-15-a');
commit(account,on,15,'on-cycle0-15-b');
assert.equal(account.fragments,balance+2); // Legitimate repeat kill retains 1 fragment each.
commit(account,on,50,'on-cycle0-50-a');
assert.equal(start(on),1);assert.equal(on.cycle,1);assert.equal(start(on),1);assert.equal(on.cycle,1);
assert.equal(rin.cycle,0);assert.equal(rin.checkpoint,0);
commit(account,on,50,'on-cycle0-50-a');assert.equal(on.readyForNext,false);
assert.equal(d.relics.definitions.length,10);
assert.equal(new Set(d.relics.definitions.map(x=>x.id)).size,10);
const maximumResonance=resonance(50,5)+d.relics.definitions.filter(x=>x.effect==='stunDamageBonus').reduce((sum,r)=>sum+r.values.at(-1),0);
assert(maximumResonance<d.combat.stunDamageMultiplierCap);
assert.equal(d.input.directionButtonBackground,false);
assert.equal(d.input.minimumHitArea,48);
assert.equal(d.progression.checkpointNavigationPrompt,false);
assert.equal(d.progression.autoAdvanceAfterKill,true);
assert.equal(d.relics.choicesPerBoss,3);
assert.equal(d.relics.maxLevel,5);
assert.equal(d.economy.duplicateGoldPerZone,undefined);
assert.equal(d.economy.upgradeExchange.autoUpgrade,false);
assert.equal(d.visual.relicListWidthFraction,1/3);
const relics=d.relics.definitions;
const effectCaps={attackPercent:.2,maxHpPercent:.25,normalStaggerPercent:.25,perfectStaggerPercent:.25,damageReduction:.15,critChanceBonus:d.combat.critChanceCap-crit(50,5),stunDamageBonus:d.combat.stunDamageMultiplierCap-resonance(50,5)};
const relicMaxima={};
for(const r of relics){
 assert.equal(r.stage,undefined);assert.equal(r.values.length,5);
 for(let n=1;n<5;n++)assert(r.values[n]>r.values[n-1],r.id+' growth must be effective');
 relicMaxima[r.effect]=(relicMaxima[r.effect]||0)+r.values.at(-1);
}
for(const [effect,value] of Object.entries(relicMaxima))assert(value<=effectCaps[effect]+1e-10,effect+' combined cap');
// Enumerate the specified uniform 3-of-10 offer: no duplicates and equal appearances.
const offers=[];
for(let a=0;a<10;a++)for(let b=a+1;b<10;b++)for(let c=b+1;c<10;c++)offers.push([a,b,c]);
assert.equal(offers.length,120);
assert(offers.every(offer=>new Set(offer).size===3));
assert(relics.every((_,id)=>offers.filter(o=>o.includes(id)).length===36));
assert.equal(offers.filter(o=>!o.includes(0)).length,84);
// Boundary examples for the documented conversion quote; browser tests exercise the actual preview.
const exchangeExamples=[[0,150,2,200,50],[49,150,2,200,99],[50,150,1,100,0],[149,150,1,100,99],[0,60,1,100,40]];
for(const [gold,cost,n,gain,remainder]of exchangeExamples){
 assert.equal(Math.ceil((cost-gold)/e.exchangeGold),n);
 assert.equal(n*e.exchangeGold,gain);assert.equal(gold+gain-cost,remainder);
}
const hpAfterRelic=(hp,oldMax,newMax)=>Math.min(newMax,hp+Math.max(0,newMax-oldMax));
assert.equal(hpAfterRelic(50,100,110),60);
assert.equal(hpAfterRelic(60,110,110),60); // Same applied level cannot heal a second time.
assert.equal(d.production.animationFrames,3*16+4*23+3*24);
const ep=d.progression.enemy;
const enemy=(stage,cycle)=>{
 const z=Math.floor((stage-1)/10),j=(stage-1)%10,k=kind(stage);
 return {
  hp:Math.round((cycle===0?ep.hpBases:ep.ngHpBases)[z]*ep.hpFloorMultiplier**j*ep.hpKindMultipliers[k]*(cycle===0?1:ep.ngHpMultiplier**(cycle-1))),
  damage:Math.ceil((cycle===0?ep.damageBases:ep.ngDamageBases)[z]*ep.damageFloorMultiplier**j*ep.damageKindMultipliers[k]*(cycle===0?1:ep.ngDamageMultiplier**(cycle-1)))
 };
};
for(const [s,,hpText,damageText] of d.progression.enemySamples){
 assert.deepEqual(enemy(s,0),{hp:Number(hpText.replaceAll(',','')),damage:Number(damageText.replaceAll(',',''))});
}
assert.deepEqual(enemy(50,0),{hp:235323,damage:1256});
assert.deepEqual(enemy(50,2),{hp:349454,damage:2087});
const bs=d.business.sensitivity;
const revenues=bs.payerRates.map((payer,i)=>Math.round(bs.mau*payer*bs.arppuKRW[i]*(1-bs.refundRate)/1.1*(1-bs.feeRate)));
assert.deepEqual(revenues,bs.netRevenueKRW);
const missing=[];
for(const name of ['index.html','Visuals/wireframes-v4.html']){
 const full=path.join(__dirname,name),html=fs.readFileSync(full,'utf8');
 for(const [,href]of html.matchAll(/(?:href|src)=["']([^"']+)["']/g)){
  if(/^(https?:|data:|#|javascript:)/.test(href))continue;
  const clean=href.split('#')[0].split('?')[0];
  if(clean==='Evidence/planning-verification-v4.json')continue; // Written by this check below.
  if(clean&&!fs.existsSync(path.resolve(path.dirname(full),clean)))missing.push({file:name,href});
 }
}
assert.deepEqual(missing,[]);
const report={date:'2026-10-21',version:d.version,growthCases,budgets,totalGold:sumGold,totalFragments:sumFragments,costPerCharacter:spent(50)*4,maximumResonance,relicMaxima,relicOfferCombinations:offers.length,relicAppearanceProbability:.3,exchangeExamples,hpGrowthPreservesMissingHp:true,enemy50NG2:enemy(50,2),animationFrames:d.production.animationFrames,revenueSensitivities:revenues,checkpointExamplesPassed:true,duplicateCommitRejected:true,repeatBossRewardPreserved:true,characterProgressIndependent:true,cycleTransitionOnce:true,missingLocalLinks:missing,limits:'Planning arithmetic and executable contract examples; not Unity save, device, payment or measured gameplay tests.'};
fs.writeFileSync(path.join(__dirname,'Evidence','planning-verification-v4.json'),JSON.stringify(report,null,2));
console.log(JSON.stringify(report));
