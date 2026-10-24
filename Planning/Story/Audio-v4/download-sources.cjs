const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const base='https://opengameart.org/';
const sources=[
 {id:'shrine',title:'Dark Shrine Loop',creator:'qubodup and yd',page:'content/dark-shrine-loop',file:'qubodup-yd-DarkShrineLoop-OpenGameArt.flac'},
 {id:'dark-theme',title:'Dark Theme',creator:'JaggedStone',page:'content/dark-theme',file:'theme_0.ogg'},
 {id:'boss',title:'Epic Boss Battle',creator:'Juhani Junkala / SubspaceAudio',page:'content/boss-battle-music',file:'Juhani Junkala - Epic Boss Battle [Seamlessly Looping].wav'},
 {id:'dungeon',title:'Dungeon Ambience',creator:'yd',page:'content/dungeon-ambience',file:'dungeon002_0.ogg'},
 {id:'bell',title:"Bell 1 O'clock",creator:'Joseph SARDIN & Axeline T.',page:'https://bigsoundbank.com/cloche-1h-s3446.html',file:'bell-3446.mp3',downloadUrl:'https://bigsoundbank.com/UPLOAD/mp3/3446.mp3'},
 {id:'weapon-impacts',title:'Medieval sound effects — Weapon impacts',creator:'Ben Jaszczak & Brian Nelson / Still North Media',page:'content/medieval-sound-effects-weapon-impacts',file:'medieval_sfx_weapon_on_weapon_1_of_2.7z',selectedFile:'Sabre Norse Sword Blade on Blade.wav',excerptStartSeconds:12.4575,excerptDurationSeconds:.65}
];
(async()=>{const out=path.join(__dirname,'Sources');fs.mkdirSync(out,{recursive:true});
 for(const s of sources){s.sourceUrl=s.page.startsWith('https:')?s.page:base+s.page;s.downloadUrl=s.downloadUrl||base+'sites/default/files/'+encodeURIComponent(s.file);s.license='CC0-1.0';s.licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/';s.checkedAt='2026-10-22';
  for(const [url,name]of [[s.sourceUrl,s.id+'-source.html'],[s.downloadUrl,s.file]]){const file=path.join(out,name);if(!fs.existsSync(file)){const response=await fetch(url);if(!response.ok)throw Error(url+' '+response.status);fs.writeFileSync(file,Buffer.from(await response.arrayBuffer()));}}
  s.sha256=crypto.createHash('sha256').update(fs.readFileSync(path.join(out,s.file))).digest('hex');console.log(s.id+' downloaded');
 }
 fs.writeFileSync(path.join(__dirname,'sources.json'),JSON.stringify(sources,null,2));
})().catch(e=>{console.error(e);process.exitCode=1});
