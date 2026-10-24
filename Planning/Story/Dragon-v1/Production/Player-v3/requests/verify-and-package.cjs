const fs = require('fs'), path = require('path'), crypto = require('crypto');
const { createCanvas, loadImage } = require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');
const base = path.resolve(__dirname, '..'), source = path.resolve(base, '../Player-v2');
const index = JSON.parse(fs.readFileSync(path.join(__dirname, 'style-plan-index.json'), 'utf8'));
const results = JSON.parse(fs.readFileSync(path.join(__dirname, 'workbench-results.json'), 'utf8'));
const hash = b => crypto.createHash('sha256').update(b).digest('hex');
async function pixels(file) { const im = await loadImage(file), c = createCanvas(im.width, im.height), x = c.getContext('2d'); x.drawImage(im, 0, 0); return { im, d: x.getImageData(0, 0, im.width, im.height).data }; }
async function main() {
  const checks = [], manifest = JSON.parse(fs.readFileSync(path.join(source, 'manifest.json'), 'utf8'));
  for (const item of index) {
    const plan = JSON.parse(fs.readFileSync(path.join(__dirname, `${item.folder}-style.json`), 'utf8'));
    const cols = 4, rows = Math.ceil(item.frames / cols), sheet = createCanvas(cols * 256, rows * 280 + 44), cx = sheet.getContext('2d');
    cx.imageSmoothingEnabled = false; cx.fillStyle = '#DBE9D5'; cx.fillRect(0, 0, sheet.width, sheet.height);
    cx.fillStyle = '#20333C'; cx.font = 'bold 22px "Malgun Gothic"'; cx.fillText(`루카 v3 · ${item.clip}`, 12, 29);
    for (let i = 0; i < item.frames; i++) {
      const name = String(i).padStart(2, '0') + '.png', oldFile = path.join(source, 'clips', item.folder, name), newFile = path.join(base, 'clips', item.folder, name);
      const old = await pixels(oldFile), now = await pixels(newFile);
      if (now.im.width !== 32 || now.im.height !== 32) throw new Error('Wrong canvas ' + newFile);
      if (hash(fs.readFileSync(oldFile)) !== plan.audit[i].sourcePngSha256) throw new Error('Source changed ' + oldFile);
      const allowed = new Set(plan.audit[i].outlineCoordinates.map(p => p.join(','))), changed = [], colors = new Set(), oldColors = new Set();
      let alphaChanged = 0, unplannedChanged = 0, protectedChanged = 0, soleChanged = 0, lowerClothPaletteChanges = 0;
      for (let y = 0; y < 32; y++) for (let x = 0; x < 32; x++) {
        const p = (y * 32 + x) * 4, a = [...old.d.slice(p, p + 4)], b = [...now.d.slice(p, p + 4)];
        if (a[3] !== b[3]) alphaChanged++;
        if (b[3]) colors.add(b.join(','));
        if (a[3]) oldColors.add(a.join(','));
        if (a.join(',') === b.join(',')) continue;
        changed.push([x, y]);
        const ac = a.join(','), bc = b.join(',');
        const permitted = (ac === '45,115,93,255' && bc === '40,119,128,255') || (ac === '139,47,53,255' && bc === '162,59,45,255') || (ac === '107,63,42,255' && bc === '32,45,56,255' && allowed.has(`${x},${y}`));
        if (!permitted) unplannedChanged++;
        if (['243,189,131,255', '171,108,55,255', '183,211,208,255', '232,217,173,255'].includes(ac)) protectedChanged++;
        if (y >= 24 && ['107,63,42,255', '32,45,56,255', '171,108,55,255'].includes(ac)) soleChanged++;
        if (y >= 24 && ac === '45,115,93,255') lowerClothPaletteChanges++;
      }
      if (alphaChanged || unplannedChanged || protectedChanged || soleChanged || colors.size !== oldColors.size || colors.size > 8) throw new Error(JSON.stringify({ item, i, alphaChanged, unplannedChanged, protectedChanged, soleChanged, colors: colors.size }));
      checks.push({ clip: item.clip, frame: i, sourcePngSha256: hash(fs.readFileSync(oldFile)), outputPngSha256: hash(fs.readFileSync(newFile)), changedPixels: changed.length, outlinePixels: plan.audit[i].outlinedPixels, alphaChanged, unplannedChanged, faceHandSteelCreamChanged: protectedChanged, footBrownNavyTanChanged: soleChanged, lowerClothPaletteChanges, paletteSize: colors.size });
      const col = i % cols, row = Math.floor(i / cols), ox = col * 256, oy = 44 + row * 280;
      cx.fillStyle = '#20333C'; cx.font = '16px monospace'; cx.fillText(String(i).padStart(2, '0'), ox + 8, oy + 18);
      cx.drawImage(now.im, ox, oy + 22, 256, 256);
    }
    fs.writeFileSync(path.join(base, 'reviews', `${item.folder}-final.png`), sheet.toBuffer('image/png'));
  }
  const actor = manifest.actors[0];
  actor.displayName = '루카'; actor.canonical = 'Planning/Story/Dragon-v1/Production/Player-v3/canonical-a-outline.png';
  actor.palette = actor.palette.map(c => c === '#2D735D' ? '#287780' : c === '#8B2F35' ? '#A23B2D' : c);
  for (const [clipName, clip] of Object.entries(actor.clips)) {
    const key = clipName === 'DodgeDown' ? 'Attack' : clipName, item = index.find(i => i.clip === key), result = results.find(r => r.folder === item.folder);
    clip.frames = clip.frames.map(p => p.replace('/Player-v2/', '/Player-v3/'));
    clip.review = `Planning/Story/Dragon-v1/Production/Player-v3/reviews/${item.folder}-final.png`;
    clip.legacyProcessedAssetId = clip.processedAssetId; clip.processedAssetId = result.gallery; clip.styleWorkbenchId = result.id;
    clip.status = 'ready';
  }
  manifest.package = 'Dragon-v1-luka-player-v3'; manifest.status = 'ready';
  manifest.styleSource = { selected: 'A', sourcePackage: 'Planning/Story/Dragon-v1/Production/Player-v2/manifest.json', npcReference: 'Planning/Story/Dragon-v1/images/actors-pixel-v3.png', originalMotionRetained: true, alphaAndFootprintChanged: false, bodyProportionsChanged: false, nativePixelsEditedBy: 'PixelLab pixelart_workbench', canonicalGalleryId: 'ba141fea-a268-4fca-ad90-53ebb8d6a5d8' };
  manifest.notes = [...manifest.notes, 'Luka v3 accepted canonical A. Only recorded exposed hair/backpack brown-edge pixels become existing navy; tunic/scarf palette mapped. No alpha, foot placement, pose, pivot, contact frame or timing changes.', '52 physical source frames; DodgeDown reuses Attack14. Idle8 has 7 unique frames.', 'Runtime grounding and all game code remain outside this art package.'];
  manifest.review = { producer: '/root/presentation_art_review', independentReviewer: '/root', decision: 'accepted-for-game-connection', scope: 'Canonical comparison and all six final source-frame sheets viewed; source geometry and timing preserved. Runtime QA is separate.', report: 'Planning/Story/Dragon-v1/Production/Player-v3/style-grounding-review.md' };
  const legacy = JSON.parse(fs.readFileSync(path.join(source, 'manifest.json'), 'utf8'));
  const fixedFields = ['fps', 'loop', 'contactFrame', 'effectOriginTopLeft', 'pivotTopLeft', 'sourceIndices', 'frameDurationsMs', 'referenceHeight'];
  for (const [name, clip] of Object.entries(actor.clips)) for (const field of fixedFields) {
    if (JSON.stringify(clip[field]) !== JSON.stringify(legacy.actors[0].clips[name][field])) throw new Error('Changed motion metadata: ' + name + '.' + field);
  }
  fs.writeFileSync(path.join(base, 'manifest.json'), JSON.stringify(manifest, null, 2));
  fs.copyFileSync(path.join(source, 'license.json'), path.join(base, 'license.json'));
  fs.writeFileSync(path.join(base, 'validation.json'), JSON.stringify({ framesChecked: checks.length, allPassed: true, invariants: ['native32', 'source hashes unchanged', 'all alpha bytes unchanged', 'only recorded exact color substitutions', 'face/hand skin, tan, steel and cream pixels unchanged', 'all foot brown/navy/tan material pixels unchanged; lower tunic pixels receive the shared palette map', 'shared8color palette, per-frame count identical to source', 'clip pivots/timing copied unchanged'], checks }, null, 2));
  console.log(JSON.stringify({ frames: checks.length, allPassed: true, manifest: path.join(base, 'manifest.json') }));
}
main().catch(e => { console.error(e); process.exitCode = 1; });
