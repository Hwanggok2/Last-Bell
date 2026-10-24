// Read-only source analysis. Produces guarded PixelLab commands, never edits source PNGs.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { createCanvas, loadImage } = require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const base = path.resolve(__dirname, '..'), source = path.resolve(base, '../Player-v2');
const manifest = JSON.parse(fs.readFileSync(path.join(source, 'manifest.json'), 'utf8'));
const aliases = { Run: 'run', Idle: 'idle', Attack: 'attack', DodgeUp: 'dodge-up', DodgeBack: 'dodge-back', Hit: 'hit' };
const hex = (d, x, y) => '#' + [...d.slice((y * 32 + x) * 4, (y * 32 + x) * 4 + 3)].map(v => v.toString(16).padStart(2, '0')).join('');
function groups(points) {
  const left = new Set(points.map(([x, y]) => `${x},${y}`)), result = [];
  while (left.size) {
    const queue = [left.values().next().value], g = []; left.delete(queue[0]);
    while (queue.length) {
      const [x, y] = queue.pop().split(',').map(Number); g.push([x, y]);
      for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) {
        const k = `${x + dx},${y + dy}`;
        if (left.has(k)) { left.delete(k); queue.push(k); }
      }
    }
    result.push(g);
  }
  return result.sort((a, b) => b.length - a.length);
}
async function main() {
  const plans = [];
  for (const [clip, folder] of Object.entries(aliases)) {
    const spec = manifest.actors[0].clips[clip], changes = [], audit = [];
    for (let frame = 0; frame < spec.frames.length; frame++) {
      const file = path.join(source, 'clips', folder, String(frame).padStart(2, '0') + '.png');
      const im = await loadImage(file), canvas = createCanvas(32, 32), ctx = canvas.getContext('2d'); ctx.drawImage(im, 0, 0);
      const d = ctx.getImageData(0, 0, 32, 32).data;
      const opaque = (x, y) => x >= 0 && x < 32 && y >= 0 && y < 32 && d[(y * 32 + x) * 4 + 3] > 0;
      const byColor = color => { const p = []; for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) if (opaque(x, y) && hex(d, x, y) === color) p.push([x, y]); return p; };
      const brown = byColor('#6b3f2a'), skin = byColor('#f3bd83'), steel = byColor('#b7d3d0'), cream = byColor('#e8d9ad');
      const face = groups(skin.filter(p => p[1] < 19))[0];
      if (!face) throw new Error('Missing face ' + file);
      const faceBottom = Math.max(...face.map(p => p[1]));
      const hairGroups = groups(brown.filter(p => p[1] <= faceBottom));
      const hair = hairGroups.find(g => Math.min(...g.map(p => p[1])) <= 10 && g.length > 20);
      if (!hair) throw new Error('Missing hair ' + file);
      const hairSet = new Set(hair.map(p => p.join(',')));
      const bag = cream.length ? [Math.min(...cream.map(p => p[0])) - 1, Math.min(...cream.map(p => p[1])) - 1, Math.max(...cream.map(p => p[0])) + 1, Math.max(...cream.map(p => p[1])) + 5] : null;
      const pixels = brown.filter(([x, y]) => {
        const region = hairSet.has(`${x},${y}`) || (bag && x >= bag[0] && x <= bag[2] && y >= bag[1] && y <= bag[3]);
        if (!region || y >= 24) return false;
        if (steel.some(([sx, sy]) => Math.abs(sx - x) <= 2 && Math.abs(sy - y) <= 2)) return false;
        return [[x - 1, y], [x + 1, y], [x, y - 1], [x, y + 1]].some(([px, py]) => !opaque(px, py));
      }).map(([x, y]) => [x, y, '#6B3F2A', '#202D38']);
      changes.push({ operation: 'patch_pixels', layer: 'image', frames: [frame + 1], pixels, reason: 'NPC-style exposed hair/backpack edge only. Preserve alpha, face/eye, sword/grip, feet, pose and all source pixels outside the recorded mask.' });
      audit.push({ frame, sourceFile: file.replaceAll('\\', '/'), sourcePngSha256: crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'), faceBottom, bagRegion: bag, outlinedPixels: pixels.length, outlineCoordinates: pixels.map(p => p.slice(0, 2)) });
    }
    changes.push({ operation: 'map_colors', layer: 'image', frames: spec.frames.map((_, i) => i + 1), region: [0, 0, 31, 31], colors: { '#2D735D': '#287780', '#8B2F35': '#A23B2D' }, reason: 'Same accepted A tunic teal/scarf warm red; no additional palette entries, no change to alpha or geometry.' });
    const plan = { clip, folder, sourceGalleryId: spec.processedAssetId, sourceJobId: spec.sourceJobId, pivotTopLeft: spec.pivotTopLeft, changes, audit };
    fs.writeFileSync(path.join(__dirname, `${folder}-style.json`), JSON.stringify(plan, null, 2));
    plans.push({ clip, folder, sourceGalleryId: spec.processedAssetId, frames: spec.frames.length, outlineCounts: audit.map(a => a.outlinedPixels) });
  }
  fs.writeFileSync(path.join(__dirname, 'style-plan-index.json'), JSON.stringify(plans, null, 2));
  console.log(JSON.stringify(plans));
}
main().catch(e => { console.error(e); process.exitCode = 1; });
