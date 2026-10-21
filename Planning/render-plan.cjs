const fs = require('node:fs');
const path = require('node:path');
const {chromium} = require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');

(async () => {
  const root = __dirname;
  const browser = await chromium.launch({headless:true, executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
  const page = await browser.newPage({viewport:{width:1440,height:1000},deviceScaleFactor:1});
  const errors=[];
  page.on('pageerror', e=>errors.push(e.message));
  const report={checkedAt:'2026-10-21',version:'planning-v2',type:'document browser rendering; no Unity or physical-device testing',screens:[],errors};
  await page.goto('file:///'+path.join(root,'index.html').replaceAll('\\','/'));
  await page.locator('.hero-art').waitFor({state:'visible'});
  await page.screenshot({path:path.join(root,'Visuals','plan-overview.png'),fullPage:false});
  for (const width of [1440,768,360]) {
    await page.setViewportSize({width,height:900});
    const result=await page.evaluate(()=>({viewport:innerWidth,documentWidth:document.documentElement.scrollWidth,missingImages:[...document.images].filter(i=>!i.complete||!i.naturalWidth).map(i=>i.getAttribute('src'))}));
    report.screens.push(result);
    if(result.documentWidth>width || result.missingImages.length) throw new Error('Document layout validation failed: '+JSON.stringify(result));
  }
  const calcBefore=await page.locator('#calc-total').innerText();
  await page.locator('#calc-mau').fill('10000');
  await page.locator('#calc-mau').dispatchEvent('input');
  const calcAfter=await page.locator('#calc-total').innerText();
  report.calculator={before:calcBefore,after:calcAfter,changed:calcBefore!==calcAfter};
  if(!report.calculator.changed) throw new Error('Revenue calculator did not update');
  await page.setViewportSize({width:1440,height:1000});
  await page.goto('file:///'+path.join(root,'Visuals','wireframes-v2.html').replaceAll('\\','/'));
  await page.screenshot({path:path.join(root,'Visuals','wireframes-preview-v2.png'),fullPage:true});
  report.wireframeScreens=[];
  for(const width of [1600,1440,768,360]){
    await page.setViewportSize({width,height:900});
    const result=await page.evaluate(()=>({viewport:innerWidth,documentWidth:document.documentElement.scrollWidth}));
    report.wireframeScreens.push(result);
    if(result.documentWidth>width)throw new Error('Wireframe horizontal clipping: '+JSON.stringify(result));
  }
  for (const [source,output] of [['wireframes-v2.svg','wireframes-sheet-v2.png'],['landscape-battle-v2.svg','landscape-battle-v2.png']]) {
    const sheet=fs.readFileSync(path.join(root,'Visuals',source),'utf8');
    const bounds=sheet.match(/viewBox="([\d.\s]+)"/)[1].trim().split(/\s+/).map(Number);
    await page.setViewportSize({width:Math.ceil(bounds[2]),height:Math.ceil(bounds[3])});
    await page.setContent('<!doctype html><html><head><meta charset="utf-8"></head><body style="margin:0">'+sheet+'</body></html>');
    await page.screenshot({path:path.join(root,'Visuals',output),fullPage:false,animations:'disabled',timeout:60000});
  }
  fs.writeFileSync(path.join(root,'Evidence','document-render-check.json'),JSON.stringify(report,null,2));
  await browser.close();
  if(errors.length)throw new Error(errors.join('\n'));
  console.log(JSON.stringify(report));
})().catch(e=>{console.error(e);process.exit(1)});
