// Read-only localhost preview: Story assets plus the exact game art referenced by this page.
// Never serve the workspace, Client source, saves, settings, or directory listings.
const fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const story=path.resolve(__dirname,'../..'),root=path.resolve(story,'../..');
const gallery=JSON.parse(fs.readFileSync(path.join(story,'MonsterMotion-v1/motions.json'),'utf8'));
const art=new Set();
for(const a of gallery.actors)for(const c of [a.baseClip,...a.clips])for(const p of c.frames||[])if(p.startsWith('../../Client/Assets/Game/Art/'))art.add('/'+p.slice(6));
const clips='/Client/Assets/Game/Art/Presentation/Clips/';
for(const p of ['hero-v5-idle/01.png','hero-dodge-up/04.png','hero-dodge-back/08.png','hero-dodge-front/04.png','hero-v6-slash-up/04.png',...Array.from({length:8},(_,i)=>'hero-v12-fast-run/'+String(i).padStart(2,'0')+'.png')])art.add(clips+p);
art.add('/Client/Assets/Game/Art/Fonts/NanumGothic-Regular.ttf');
const mime={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.json':'application/json; charset=utf-8','.png':'image/png','.gif':'image/gif','.jpg':'image/jpeg','.ttf':'font/ttf','.txt':'text/plain; charset=utf-8','.wav':'audio/wav','.ogg':'audio/ogg','.mp3':'audio/mpeg'};
http.createServer((req,res)=>{
 if(!['GET','HEAD'].includes(req.method)){res.writeHead(405).end();return;}
 let url;try{url=decodeURIComponent(new URL(req.url,'http://localhost').pathname);}catch{res.writeHead(400).end();return;}
 if(url==='/')url='/route-prologue-tutorial-v4.html';
 let file;
 if(url.startsWith('/Client/')){if(art.has(url))file=path.resolve(root,'.'+url);}
 else if(!url.split('/').some(s=>s.startsWith('.'))){const candidate=path.resolve(story,'.'+url);if(candidate.startsWith(story+path.sep)&&mime[path.extname(candidate)])file=candidate;}
 if(!file||!fs.existsSync(file)||!fs.statSync(file).isFile()){res.writeHead(404).end();return;}
 res.writeHead(200,{'Content-Type':mime[path.extname(file)]||'application/octet-stream','Cache-Control':'no-store'});
 if(req.method==='HEAD')res.end();else fs.createReadStream(file).pipe(res);
}).listen(8875,'127.0.0.1',()=>console.log('Preview only: http://127.0.0.1:8875/route-prologue-tutorial-v4.html (Story + '+art.size+' whitelisted art files)'));
