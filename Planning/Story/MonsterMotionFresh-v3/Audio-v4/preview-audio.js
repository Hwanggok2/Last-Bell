'use strict';
(() => {
  const base=new URL('.',document.currentScript.src),panel=document.getElementById('sound-controls');
  if(!panel)return;
  panel.innerHTML='<div class="controls"><button id="audio-toggle" type="button" aria-pressed="false">소리 켜기</button><label>배경음 <select id="audio-mood" aria-label="배경음 선택"><option value="auto">장면에 맞게</option><option value="bgm-road">성문 밖 · 긴장</option><option value="bgm-sanctuary">등불 아래 · 휴식</option><option value="bgm-boss">가디언 · 보스전</option><option value="bgm-quarter">성문 안 · 폐허</option><option value="off">배경음 끄기</option></select></label><button id="audio-attack" type="button">검 충돌음 듣기</button><button id="audio-bell" type="button">낮은 종 듣기</button></div><div class="controls"><label>전체 음량 <input id="audio-master" aria-label="전체 음량" type="range" min="0" max="100" value="70"></label><label>음악 음량 <input id="audio-music" aria-label="음악 음량" type="range" min="0" max="100" value="35"></label></div><p id="audio-status" class="muted" role="status">소리를 켜면 공격 모션과 함께 재생합니다.</p><details><summary>음원 출처</summary><p>Dark Theme — JaggedStone · Dark Shrine Loop — qubodup / yd · Epic Boss Battle — Juhani Junkala · Dungeon Ambience — yd · 종 녹음 — Joseph SARDIN &amp; Axeline T. · 검 충돌 — Ben Jaszczak &amp; Brian Nelson · 기타 효과음 — Kenney. 모두 CC0 원본을 게임에 맞게 편집했습니다. <a id="audio-credits">전체 출처와 편집 내역</a></p></details>';
  const $=id=>document.getElementById(id);$('audio-credits').href=new URL('CREDITS.txt',base);
  let context,master,musicBus,sfxBus,manifest,enabled=false,paused=false,mood='bgm-road',music=null,musicKey=null,generation=0,starting=null;
  const buffers=new Map(),voices=new Set();
  const data=fetch(new URL('delivery.json',base),{cache:'no-store'}).then(r=>{if(!r.ok)throw Error('음원 목록 '+r.status);return r.json();}).then(d=>{manifest=d;return d;});
  data.catch(()=>{$('audio-status').textContent='음원 목록을 불러오지 못했습니다. 로컬 미리보기 서버에서 열어 주세요.';});
  const record=key=>manifest.files.find(f=>f.key===key);
  function status(){const key=$('audio-mood').value==='auto'?mood:$('audio-mood').value;$('audio-status').textContent=enabled?`소리 켜짐 · ${key==='off'?'효과음만':record(key)?.title||'준비 중'}${paused?' · 일시정지':''}`:'소리 꺼짐';$('audio-toggle').textContent=enabled?'소리 끄기':'소리 켜기';$('audio-toggle').setAttribute('aria-pressed',String(enabled));}
  async function buffer(key){if(!buffers.has(key))buffers.set(key,fetch(new URL(record(key).file+'?v='+record(key).sha256.slice(0,12),base)).then(r=>{if(!r.ok)throw Error(key+' '+r.status);return r.arrayBuffer();}).then(b=>context.decodeAudioData(b)));return buffers.get(key);}
  function stopSfx(){for(const v of voices){v.source.stop();}voices.clear();}
  function pause(value){paused=value;stopSfx();if(context&&enabled){if(paused||document.hidden)context.suspend();else context.resume();}status();}
  async function setMood(next){mood=next;if(!enabled)return;const key=$('audio-mood').value==='auto'?mood:$('audio-mood').value;if(key===musicKey){status();return;}const token=++generation;musicKey=key;
    try{const decoded=key==='off'?null:await buffer(key);if(token!==generation||!enabled)return;
      const at=context.currentTime;if(music){music.gain.gain.cancelScheduledValues(at);music.gain.gain.setValueAtTime(music.gain.gain.value,at);music.gain.gain.linearRampToValueAtTime(0,at+.7);music.source.stop(at+.75);music=null;}
      if(decoded){const source=context.createBufferSource(),gain=context.createGain();source.buffer=decoded;source.loop=true;source.connect(gain).connect(musicBus);gain.gain.setValueAtTime(0,at);gain.gain.linearRampToValueAtTime(1,at+.7);source.start();source.onended=()=>{source.disconnect();gain.disconnect();};music={source,gain};}status();
    }catch(error){musicKey=null;$('audio-status').textContent='음악을 불러오지 못했습니다: '+error.message;}
  }
  async function enable(){if(enabled)return true;if(starting)return starting;
    if(!context){context=new AudioContext();master=context.createGain();musicBus=context.createGain();sfxBus=context.createGain();musicBus.connect(master);sfxBus.connect(master);const limiter=context.createDynamicsCompressor();limiter.threshold.value=-6;limiter.knee.value=3;limiter.ratio.value=20;limiter.attack.value=.002;limiter.release.value=.15;master.connect(limiter).connect(context.destination);master.gain.value=Number($('audio-master').value)/100;musicBus.gain.value=Number($('audio-music').value)/100;}
    context.resume();$('audio-toggle').disabled=true;$('audio-status').textContent='소리 준비 중…';
    starting=(async()=>{try{await data;await Promise.all(manifest.files.filter(f=>f.kind==='sfx').map(f=>buffer(f.key)));enabled=true;if(paused||document.hidden)context.suspend();await setMood(mood);status();return true;}catch(error){enabled=false;$('audio-status').textContent='소리를 불러오지 못했습니다: '+error.message;return false;}finally{starting=null;$('audio-toggle').disabled=false;}})();return starting;
  }
  function play(key,volume=1,delay=0){if(!enabled||paused||document.hidden||context.state!=='running')return;
    const spec=record(key);if(!spec)return;
    // Buffers are loaded before enabling. Guard async completion against mute/pause.
    buffer(key).then(decoded=>{if(!enabled||paused||document.hidden||context.state!=='running')return;
      const same=[...voices].filter(v=>v.key===key),limit=key==='bell-low-long'?1:3;
      while(same.length>=limit){const old=same.shift();old.source.stop();voices.delete(old);}
      const source=context.createBufferSource(),gain=context.createGain();source.buffer=decoded;gain.gain.value=spec.gain*volume;source.connect(gain).connect(sfxBus);const voice={source,key};voices.add(voice);source.onended=()=>{voices.delete(voice);source.disconnect();gain.disconnect();};source.start(context.currentTime+delay);
    });
  }
  $('audio-toggle').onclick=async()=>{if(enabled){enabled=false;generation++;stopSfx();context.suspend();status();}else{musicKey=null;await enable();}};
  $('audio-mood').onchange=()=>setMood(mood);
  $('audio-master').oninput=()=>{if(master)master.gain.setTargetAtTime(Number($('audio-master').value)/100,context.currentTime,.02);};
  $('audio-music').oninput=()=>{if(musicBus)musicBus.gain.setTargetAtTime(Number($('audio-music').value)/100,context.currentTime,.08);};
  $('audio-attack').onclick=async()=>{if(await enable())play('sword-clash');};
  $('audio-bell').onclick=async()=>{if(await enable())play('bell-low-long');};
  document.addEventListener('visibilitychange',()=>{stopSfx();if(context&&enabled){if(document.hidden||paused)context.suspend();else context.resume();}});
  window.LastBellAudio={play,setMood,stopSfx,pause};
})();
