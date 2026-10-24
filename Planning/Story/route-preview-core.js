'use strict';
/* Review-only timing model. No game data or browser save is modified. */
(function(root) {
  const cueSlot=720, gap=480, normal=160, perfect=60, reuse=260;
  const directions={low:'up',high:'down',thrust:'left',rear:'right'};
  function events(sequence=['low','high']) {
    return sequence.map((type,index)=>({type,index,cueStart:index*cueSlot,
      blinks:[0,240,480].map(t=>index*cueSlot+t),impact:sequence.length*cueSlot+index*gap,
      direction:directions[type]}));
  }
  class Trial {
    constructor(sequence=['low','high']) {this.events=events(sequence);this.time=0;this.lastInput=-Infinity;this.inputs=[];this.hits=[];this.pose=null;}
    advance(t) {
      this.time=Math.max(this.time,t);
      for (const e of this.events) if(this.time>=e.impact&&!this.hits.some(h=>h.index===e.index)) {
        const input=[...this.inputs].reverse().find(i=>i.index===e.index);
        this.hits.push({index:e.index,time:e.impact,type:e.type,result:input?.result||'miss'});
      }
      return this.hits;
    }
    input(direction,t) {
      this.advance(Math.max(0,t-0.0001));
      if(t-this.lastInput<reuse) return {result:'cooldown'};
      const e=this.events.find(e=>t<=e.impact&&!this.hits.some(h=>h.index===e.index));
      if(!e) return {result:'finished'};
      this.lastInput=t;
      const early=e.impact-t;
      const result=direction!==e.direction?'wrong':early>normal?'early':early<=perfect?'perfect':'normal';
      const input={direction,index:e.index,time:t,early,result};this.inputs.push(input);this.pose=input;this.advance(t);
      return input;
    }
    get remaining() {return this.events.filter(e=>this.time<e.impact).length;}
    get finished() {return this.time>=this.events.at(-1).impact+360;}
    get success() {return this.hits.length===this.events.length&&this.hits.every(h=>h.result==='normal'||h.result==='perfect');}
  }
  function gate(stage) {
    if(stage>=12) return {side:'absent',x:-350,size:1,open:true};
    if(stage===11) return {side:'left-inside',x:16,size:1,open:true};
    return {side:'right-closed',x:stage===10?740:850-(stage-1)*8,size:stage===10?1:0.24+stage*0.055,open:false};
  }
  function cueAt(t,es=events()) {
    return es.map(e=>({...e,local:t-e.cueStart})).find(e=>e.local>=0&&e.local<720)||null;
  }
  const api={Trial,events,gate,cueAt,directions,cueSlot,gap,normal,perfect,reuse};
  if(typeof module!=='undefined'&&module.exports) module.exports=api;
  else root.RoutePreviewCore=api;
})(typeof window==='undefined'?globalThis:window);
