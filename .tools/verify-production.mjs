import { chromium } from 'file:///C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright/index.mjs';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';
const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
const page=await browser.newPage({viewport:{width:1440,height:1000},deviceScaleFactor:1});
await page.goto('file:///C:/Github/Test2/Planning/Production/index.html');
await page.locator('img').evaluateAll(items=>Promise.all(items.map(x=>x.decode().catch(()=>{}))));
const audit=await page.evaluate(()=>({
  title:document.title, overflow:document.documentElement.scrollWidth>innerWidth,
  zones:document.querySelectorAll('.zone').length,
  brokenImages:[...document.images].filter(i=>!i.naturalWidth).map(i=>i.src),
  links:[...document.querySelectorAll('a')].map(a=>a.href)
}));
audit.brokenLinks=audit.links.filter(u=>u.startsWith('file:')&&!fs.existsSync(fileURLToPath(new URL(u.split('#')[0]))));
delete audit.links;
await page.screenshot({path:'C:/Github/Test2/Planning/Evidence/production-board.png'});
fs.writeFileSync('C:/Github/Test2/Planning/Evidence/production-board-check.json',JSON.stringify(audit,null,2));
console.log(JSON.stringify(audit));
await browser.close();
if(audit.overflow||audit.brokenImages.length||audit.brokenLinks.length||audit.zones!==5)process.exitCode=1;
