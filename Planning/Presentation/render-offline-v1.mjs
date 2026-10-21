// Offline Skia rendering of owned Canvas drawing functions. No browser, UI automation or network.
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
const require=createRequire(import.meta.url);
const moduleDir=process.env.LASTBELL_NODE_MODULES || 'C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules';
const {createCanvas,loadImage,GlobalFonts}=require(path.join(moduleDir,'@napi-rs/canvas'));
const here=path.dirname(fileURLToPath(import.meta.url));
const out=path.resolve(here,'../Evidence/Presentation');fs.mkdirSync(out,{recursive:true});
GlobalFonts.registerFromPath(path.resolve(here,'../../Client/Assets/Game/Art/Fonts/NanumGothic-Regular.ttf'),'Bell');
const ctx=vm.createContext({window:{},document:{getElementById:()=>null},console});
vm.runInContext(fs.readFileSync(path.join(here,'data-v1.js'),'utf8'),ctx);
const drawing=fs.readFileSync(path.join(here,'app-v1.js'),'utf8').split('let raf=')[0];
vm.runInContext(drawing,ctx);
const paths=vm.runInContext('paths',ctx),loaded={};
for(const [key,file] of Object.entries(paths))loaded[key]=await loadImage(path.resolve(here,file));
ctx.loaded=loaded;vm.runInContext('Object.assign(images,loaded)',ctx);
function frame(kind,args){const canvas=createCanvas(1280,720);ctx.canvas=canvas;ctx.args=args;const caption=vm.runInContext(`${kind}(canvas,...args)`,ctx);return{canvas,caption}}
function write(name,canvas){fs.writeFileSync(path.join(out,name+'.png'),canvas.toBuffer('image/png'))}
function sheet(title,items,cols,file){const width=1280,cell=width/cols,rows=Math.ceil(items.length/cols),row=cell*9/16+58;const canvas=createCanvas(width,86+rows*row),g=canvas.getContext('2d');g.fillStyle='#0b1422';g.fillRect(0,0,canvas.width,canvas.height);g.fillStyle='#eed49d';g.font='bold 27px Bell';g.fillText(title,24,49);g.font='16px Bell';g.imageSmoothingEnabled=false;for(let i=0;i<items.length;i++){const x=i%cols*cell,y=86+Math.floor(i/cols)*row;g.drawImage(items[i].canvas,x,y,cell,cell*9/16);g.fillStyle='#c4cedc';g.fillText(items[i].label,x+16,y+cell*9/16+33)}write(file,canvas)}
const before=frame('scaleFrame',[1]),after=frame('scaleFrame',[1.5]);sheet('재의 불씨 · 동일 카메라 / 크기 비교',[{...before,label:'현재 · 높이1.0 / 온의0.91배'},{...after,label:'제안 · 높이1.5 / 현재보다50% 확대'}],2,'scale-comparison');
const warning=[];for(const [id,name]of [['low_sweep','하단 → ▲'],['high_sweep','상단 → ▼'],['thrust','정면 → ◀'],['outer_arc','후방 → ▶']]){const f=frame('warningFrame',[id,.72,'brass',false,false]);warning.push({...f,label:name});write('warning-'+id,f.canvas)}sheet('금 간 종의 잔향 · 4방향 초안',warning,2,'warning-directions');
for(const [id,mode,next,mono,name]of [['low_sweep','current',false,false,'warning-current'],['low_sweep','ornament',false,false,'warning-ornament'],['low_sweep','brass',true,false,'warning-next'],['outer_arc','brass',true,true,'warning-mono'],['high_sweep','wire',false,true,'wireframe']])write(name,frame('warningFrame',[id,.8,mode,next,mono]).canvas);
let sequenceItems=[];const sequences=[['normal',[200,450,850,1400],'처치·전진'],['death',[300,700,1100,1800],'사망·등불'],['mini',[0,450,1000,1400],'중간 보스'],['boss',[0,600,1250,1800],'보스']];
const captions=[];for(const[key,times,label]of sequences){for(const t of times){const f=frame('sequenceFrame',[key,t,false,false]);sequenceItems.push({...f,label:`${label} ${(t/1000).toFixed(2)}s`});captions.push({key,t,caption:f.caption});if(t===times[2]||t===times[3])write(key+'-'+t,f.canvas)}}sheet('연출 타임라인 · 시간·배치 모형 / 실제 애니메이션 아님',sequenceItems,4,'sequence-storyboard');
const reduced=frame('sequenceFrame',['boss',600,true,false]);write('boss-reduced-motion',reduced.canvas);
// Static source integrity, bounds and draw-path checks; not a browser interaction test.
const html=fs.readFileSync(path.join(here,'index-v1.html'),'utf8');const refs=[...html.matchAll(/(?:href|src)="([^"]+)"/g)].map(m=>m[1]).filter(x=>!x.startsWith('#')&&!/^https?:/.test(x));
const missing=refs.filter(x=>!fs.existsSync(path.resolve(here,x.split('#')[0])));
const plan=JSON.parse(fs.readFileSync(path.join(here,'proposal-v1.json'),'utf8'));
const checks={scope:'offline-canvas-and-static-only',date:'2026-10-21',unityModified:false,browserRendered:false,browserReason:'In-app browser file URL policy blocked automatic opening; no alternate browser navigation used.',missingLinks:missing,spriteCount:Object.keys(ctx.window.ATLAS).length,directions:warning.length,sequences:sequences.length,allFramesRendered:sequenceItems.length,proposedScaleRatio:plan.monster.proposedHeight/plan.monster.currentHeight,notes:['Browser layout and interactive controls still require a user-opened page or a separately authorized preview surface.','Static drawing functions rendered offline with Skia. No Unity/device validation claimed.'],captions};
fs.writeFileSync(path.join(out,'verification.json'),JSON.stringify(checks,null,2));console.log(JSON.stringify({...checks,captions:undefined},null,2));if(missing.length)process.exitCode=1;
