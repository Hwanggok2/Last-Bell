const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const manifestFile = process.argv[2] || path.join(root, 'delivery.json');
const destination = process.argv[3] || path.join(root, 'preview.html');
const delivery = JSON.parse(fs.readFileSync(manifestFile, 'utf8').replace(/^\uFEFF/, ''));
const images = {};
for (const actor of delivery.actors) {
  for (const file of [actor.base, ...actor.clips.flatMap(c => c.frames || [])]) {
    images[file] = 'data:image/png;base64,' + fs.readFileSync(path.join(root, file)).toString('base64');
  }
}
const template = fs.readFileSync(path.join(__dirname, 'preview-template.html'), 'utf8');
const audioRoot=path.resolve(root,'../Audio-v4'),audioOut=path.join(path.dirname(destination),'Audio-v4');
fs.mkdirSync(audioOut,{recursive:true});
for(const file of ['preview-audio.js','delivery.json','CREDITS.txt'])fs.copyFileSync(path.join(audioRoot,file),path.join(audioOut,file));
fs.cpSync(path.join(audioRoot,'Audio'),path.join(audioOut,'Audio'),{recursive:true});
fs.writeFileSync(destination, template.replace('__DELIVERY__', JSON.stringify(delivery)).replace('__IMAGES__', JSON.stringify(images)));
console.log(`Built ${destination}: ${delivery.actors.length} actors, ${Object.keys(images).length} PNGs embedded.`);
