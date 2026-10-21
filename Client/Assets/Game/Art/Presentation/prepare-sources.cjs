// Explicit offline source preparation; does not run in Unity. Run from repository root.
// Original Planning files are read-only. No rescale/redraw/palette modification is performed.
const fs = require('fs'), path = require('path'), crypto = require('crypto'), vm = require('vm');
const root = 'Assets/Game/Art/Presentation', disk = 'Client/' + root, planning = 'Planning/Presentation/PixelLab';
const sha = b => crypto.createHash('sha256').update(b).digest('hex');
const guid = p => {
  const meta = 'Client/' + p + '.meta';
  return fs.existsSync(meta) ? fs.readFileSync(meta, 'utf8').match(/guid: ([a-f0-9]+)/)[1] : crypto.createHash('md5').update('lastbell-presentation:' + p).digest('hex');
};
function meta(p, extra = '') {
  if (!fs.existsSync('Client/' + p + '.meta')) fs.writeFileSync('Client/' + p + '.meta', 'fileFormatVersion: 2\nguid: ' + guid(p) + '\n' + extra);
}
function mkdir(p) {
  fs.mkdirSync('Client/' + p, { recursive: true });
  for (let n = p; n.startsWith(root); n = n.slice(0, n.lastIndexOf('/'))) meta(n, 'folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n');
}
const ctx = { window: {} }; vm.createContext(ctx); vm.runInContext(fs.readFileSync(planning + '/assets.js', 'utf8'), ctx);
const clips = new Map(ctx.window.PIXELLAB_CLIPS.clips.map(c => [c.id, c]));
const manifest = { version: 1, sourceManifest: planning + '/assets.js', scope: '1-10 stage MVP existing selected PNGs. Monster Idle and Stun are static fallback poses, not completed continuous animation.', textures: [], actors: [], diagonalSlash: [] };
const textureMap = new Map(), template = fs.readFileSync('Client/Assets/Game/Art/Source/gate-background-v1.png.meta', 'utf8');
function texture(source, dest, pivot, buffer, sourceRect) {
  if (textureMap.has(dest)) return dest;
  const bytes = buffer || fs.readFileSync(source), width = bytes.readUInt32BE(16), height = bytes.readUInt32BE(20);
  mkdir(path.posix.dirname(dest)); fs.writeFileSync('Client/' + dest, bytes);
  let text = template.replace(/^guid: .*$/m, 'guid: ' + guid(dest)).replace(/^  alignment: .*$/m, '  alignment: 9')
    .replace(/^  spritePivot: .*$/m, `  spritePivot: {x: ${pivot[0] / width}, y: ${1 - pivot[1] / height}}`)
    .replace(/^  spriteMeshType: .*$/m, '  spriteMeshType: 0').replace(/^  spriteGenerateFallbackPhysicsShape: .*$/m, '  spriteGenerateFallbackPhysicsShape: 0')
    .replace(/^    spriteID: .*$/m, '    spriteID: ' + crypto.createHash('md5').update(dest + ':sprite').digest('hex'));
  fs.writeFileSync('Client/' + dest + '.meta', text);
  const item = { path: dest, source, sha256: sha(bytes), width, height, pivot: { x: pivot[0], y: pivot[1] } };
  if (sourceRect) { item.sourceRect = sourceRect; item.sourceSheetSha256 = sha(fs.readFileSync(source)); }
  textureMap.set(dest, item); manifest.textures.push(item); return dest;
}
function selected(id, motion) {
  const c = clips.get(id); if (!c) throw Error(id);
  const sequence = c.playbackFrames || Array.from({ length: c.frameCount }, (_, i) => i);
  const target = i => texture(`${planning}/clips/${id}/frames/${String(i).padStart(2, '0')}.png`, `${root}/Clips/${id}/${String(i).padStart(2, '0')}.png`, c.pivot);
  return { motion, sourceId: id, sourceJob: c.sourceJob || '', note: c.note || '', frames: sequence.map(target), sourceFrames: sequence,
    fps: c.fps, loop: !!c.loop, contactFrame: c.contactSourceFrame === undefined ? -1 : sequence.indexOf(c.contactSourceFrame), referenceHeightPixels: 0,
    lowPoseFrames: (c.lowPoseFrames || []).map(target), recoverFrames: (c.recoverFrames || []).map(target),
    faceAnchors: sequence.map(i => c.faceByFrame?.[i] || c.faceAnchor || [c.pivot[0], 46]).map(p => ({ x: p[0], y: p[1] })),
    effectOrigin: { x: c.effectOrigin?.[0] || 0, y: c.effectOrigin?.[1] || 0 }, staticFallback: false, legacyStun: false };
}
async function main() {
  const currentPath=disk+'/manifest.json';
  if(fs.existsSync(currentPath)&&JSON.parse(fs.readFileSync(currentPath,'utf8')).actors.some(a=>a.clips.some(c=>/^hero-(v1[12]-|approved-dash-)/.test(c.sourceId))))throw Error('A newer run is already authored. Use PresentationArtImporter.MigrateExisting to import the current preserved manifest; the legacy v10 prep cannot replace it.');
  mkdir(root);
  const actors = [['hero_on', 'hero', 1.1, 84], ['gate_sentinel', 'sentinel', 2, 86], ['chain_thrall', 'chain', 2, 86], ['ash_wisp', 'wisp', 2, 88], ['gate_warden', 'warden', 3.2, 92], ['bell_knight', 'knight', 4, 94]];
  for (const [id, name, height, referenceHeightPixels] of actors) {
    const motions = [];
    if (name === 'hero') {
      for (const [motion, source] of [['Idle', 'hero-v5-idle'], ['Run', 'hero-v10-blademage-run'], ['AttackUp', 'hero-v6-slash-up'], ['AttackDown', 'hero-v6-slash-down'], ['DodgeUp', 'hero-dodge-up'], ['DodgeDown', 'hero-dodge-down'], ['DodgeBack', 'hero-dodge-back'], ['DodgeForward', 'hero-dodge-front'], ['Hit', 'hero-v5-hit'], ['Death', 'hero-v5-death'], ['Return', 'hero-v5-return']]) motions.push(selected(source, motion));
    } else {
      const frame = texture(`${planning}/inputs/${name}.png`, `${root}/Clips/${name}-idle/00.png`, [name === 'wisp' ? 76 : 78, 114]);
      motions.push({ motion: 'Idle', sourceId: name + '-canonical-static-idle', sourceJob: '', note: 'Static canonical original; no continuous monster Idle has been produced.', frames: [frame], sourceFrames: [0], fps: 1, loop: true, contactFrame: -1, referenceHeightPixels: 0, lowPoseFrames: [], recoverFrames: [], faceAnchors: [{ x: name === 'warden' ? 68 : name === 'knight' ? 75 : 76, y: 46 }], effectOrigin: { x: 0, y: 0 }, staticFallback: true, legacyStun: false });
      motions.push(selected(name === 'knight' ? 'knight-attack-retry' : name + '-attack', 'Attack'));
      const hit = selected(name + '-v6-hit-clean', 'Hit'); motions.push(hit);
      motions.push({ ...hit, motion: 'Death', note: 'Existing recoil frames reused beneath dissolve. No separate monster death clip generated.' });
    }
    motions.push({ motion: 'Stun', sourceId: 'legacy-' + id + '-static-stun', sourceJob: '', note: 'Existing prototype single Stun pose; no continuous Stun clip exists.', frames: [], sourceFrames: [], fps: 1, loop: true, contactFrame: -1, referenceHeightPixels: 0, lowPoseFrames: [], recoverFrames: [], faceAnchors: [], effectOrigin: { x: 0, y: 0 }, staticFallback: true, legacyStun: true });
    manifest.actors.push({ id, facesRight: name === 'hero', height, referenceHeightPixels, clips: motions });
  }
  // Existing authored slash atlas: integer cell extraction using its original recipe.
  const canvasLib = process.env.LASTBELL_CANVAS_MODULE || 'C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas';
  const { createCanvas, loadImage } = require(canvasLib), source = planning + '/clips/diagonal-slash/sheet.png';
  const image = await loadImage(source), sheet = createCanvas(image.width, image.height), sheetContext = sheet.getContext('2d'); sheetContext.drawImage(image, 0, 0);
  const recipe = JSON.parse(fs.readFileSync(planning + '/clips/diagonal-slash/recipe.json', 'utf8'));
  for (let i = 0; i < recipe.frames.length; i++) {
    const r = recipe.frames[i].frame, cell = createCanvas(r.w, r.h);
    cell.getContext('2d').putImageData(sheetContext.getImageData(r.x, r.y, r.w, r.h), 0, 0);
    manifest.diagonalSlash.push(texture(source, `${root}/Clips/diagonal-slash/${String(i).padStart(2, '0')}.png`, [12, 110], cell.toBuffer('image/png'), r));
  }
  fs.writeFileSync(disk + '/manifest.json', JSON.stringify(manifest, null, 2) + '\n');
  meta(root + '/manifest.json', 'TextScriptImporter:\n  externalObjects: {}\n');
  meta(root + '/SpriteWhiteFlash.shader', 'ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n');
  meta('Assets/Game/Editor/PresentationArtImporter.cs', 'MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n');
  writeSavedAssets();
  console.log(JSON.stringify({ copiedPNGs: manifest.textures.length, actors: manifest.actors.length, motions: manifest.actors.reduce((n, a) => n + a.clips.length, 0), catalog: root + '/PresentationArtCatalog.asset' }));
}
function writeSavedAssets() {
  const mat = root + '/PresentationSprite.mat', catalog = root + '/PresentationArtCatalog.asset';
  meta(mat, 'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n');
  meta(catalog, 'NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n');
  fs.writeFileSync('Client/' + mat, `%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_Name: PresentationSprite\n  m_Shader: {fileID: 4800000, guid: ${guid(root + '/SpriteWhiteFlash.shader')}, type: 3}\n  m_Parent: {fileID: 0}\n  m_ModifiedSerializedProperties: 0\n  m_ValidKeywords:\n  - PIXELSNAP_ON\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n  stringTagMap: {}\n  disabledShaderPasses: []\n  m_LockedProperties: \n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs:\n    - _MainTex:\n        m_Texture: {fileID: 0}\n        m_Scale: {x: 1, y: 1}\n        m_Offset: {x: 0, y: 0}\n    m_Ints: []\n    m_Floats:\n    - PixelSnap: 1\n    - _Flash: 0\n    - _Dissolve: 0\n    - _EnableExternalAlpha: 0\n    m_Colors:\n    - _Color: {r: 1, g: 1, b: 1, a: 1}\n    - _RendererColor: {r: 1, g: 1, b: 1, a: 1}\n    - _Flip: {r: 1, g: 1, b: 1, a: 1}\n  m_BuildTextureStacks: []\n`);
  const sprite = p => `{fileID: 21300000, guid: ${guid(p)}, type: 3}`, material = `{fileID: 2100000, guid: ${guid(mat)}, type: 2}`;
  const list = (field, items, indent = 4) => { const pad = ' '.repeat(indent); return items.length ? `${pad}${field}:\n` + items.map(i => `${pad}- ${i}\n`).join('') : `${pad}${field}: []\n`; };
  const stunIDs = ['-6609881236248229933', '317748415028096417', '-2464838755124118109', '-4658551166589717869', '9019926859475258984', '8883373813876935054'], legacyHeights = [286, 220, 225, 268, 293, 353];
  const motionNames = ['Idle', 'Run', 'Attack', 'AttackUp', 'AttackDown', 'Hit', 'Stun', 'DodgeUp', 'DodgeBack', 'DodgeDown', 'DodgeForward', 'Return', 'Death', 'Roar'];
  let text = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: 6ee81ad5985b7f54583b1d48abd0a132, type: 3}\n  m_Name: PresentationArtCatalog\n  m_EditorClassIdentifier: LastBell.Runtime::LastBell.ArtCatalog\n  actors:\n';
  manifest.actors.forEach((a, index) => {
    const stun = `{fileID: ${stunIDs[index]}, guid: ${index === 0 ? '834027c33ed54864d9155acbca7f6619' : 'f846594fddd735f4bafee0487516413b'}, type: 3}`, get = motion => a.clips.find(c => c.motion === motion);
    text += `  - id: ${a.id}\n`;
    for (const [field, motion] of [['idle', 'Idle'], ['attack', index === 0 ? 'AttackUp' : 'Attack'], ['stun', 'Stun'], ['dodgeUp', 'DodgeUp'], ['dodgeDown', 'DodgeDown'], ['dodgeBack', 'DodgeBack'], ['dodgeForward', 'DodgeForward']]) text += list(field, motion === 'Stun' ? [stun] : (get(motion)?.frames || []).map(sprite));
    text += `    facesRight: ${a.facesRight ? 1 : 0}\n    height: ${a.height}\n    referenceHeightPixels: ${a.referenceHeightPixels}\n    material: ${material}\n    clips:\n`;
    for (const c of a.clips) {
      const packed = Buffer.alloc(c.sourceFrames.length * 4); c.sourceFrames.forEach((n, i) => packed.writeInt32LE(n, i * 4));
      text += `    - motion: ${motionNames.indexOf(c.motion)}\n      sourceId: ${c.sourceId}\n` + list('frames', c.legacyStun ? [stun] : c.frames.map(sprite), 6)
        + `      sourceFrames: ${packed.toString('hex')}\n      fps: ${c.fps}\n      loop: ${c.loop ? 1 : 0}\n      contactFrame: ${c.contactFrame}\n      referenceHeightPixels: ${c.legacyStun ? legacyHeights[index] : 0}\n`
        + list('lowPoseFrames', c.lowPoseFrames.map(sprite), 6) + list('recoverFrames', c.recoverFrames.map(sprite), 6) + list('faceAnchors', c.faceAnchors.map(v => `{x: ${v.x}, y: ${v.y}}`), 6)
        + `      effectOrigin: {x: ${c.effectOrigin.x}, y: ${c.effectOrigin.y}}\n      staticFallback: ${c.staticFallback ? 1 : 0}\n`;
    }
  });
  text += '  gateBackground: {fileID: 21300000, guid: c44f826b42df6c9408aa2c3018cfa70e, type: 3}\n  uiFont: {fileID: 12800000, guid: fbe25c357e4ff56449eb3cda3bbd1e0c, type: 3}\n  actorMaterial: ' + material + '\n' + list('diagonalSlash', manifest.diagonalSlash.map(sprite), 2);
  fs.writeFileSync('Client/' + catalog, text);
  const prefab = root + '/PresentationActor.prefab';
  let pf = fs.readFileSync('Client/Assets/Game/Prefabs/Actor.prefab', 'utf8').replace(/m_Name: Actor\r?\n/, 'm_Name: PresentationActor\n')
    .replace('  SpriteOffset: {x: 0, y: 0.55, z: 0}', `  SpriteOffset: {x: 0, y: 0, z: 0}\n  DefaultCatalog: {fileID: 11400000, guid: ${guid(catalog)}, type: 2}\n  DefaultActorId: hero_on\n  DefaultFacesRight: 1`)
    .replace('  m_LocalScale: {x: 0.55, y: 1.1, z: 1}', '  m_LocalScale: {x: 1.3095238, y: 1.3095238, z: 1}')
    .replace('  - {fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}', '  - ' + material)
    .replace('  m_Sprite: {fileID: 10905, guid: 0000000000000000f000000000000000, type: 0}', '  m_Sprite: ' + sprite(manifest.actors[0].clips[0].frames[0]))
    .replace('  m_Color: {r: 0.94, g: 0.83, b: 0.62, a: 1}', '  m_Color: {r: 1, g: 1, b: 1, a: 1}');
  fs.writeFileSync('Client/' + prefab, pf); meta(prefab, 'PrefabImporter:\n  externalObjects: {}\n');
}
main().catch(error => { console.error(error); process.exitCode = 1; });
