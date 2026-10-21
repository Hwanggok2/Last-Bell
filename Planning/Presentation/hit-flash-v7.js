/* Planning-only sprite tint. Replaces one sprite draw; never overlays the scene.
 * Call with elapsed time since a confirmed hit, not since the current frame.
 * No timer, damage, hit-stop, invulnerability, or animation-state ownership. */
(function (root) {
  'use strict';
  const HOLD_MS = 45, END_MS = 120;
  let sheets = new WeakMap(), scratch = null;
  let canvasFactory = (width, height) => {
    if (!root.document || typeof root.document.createElement !== 'function') {
      throw new Error('HitFlashV7: inject a canvas factory in the offline renderer.');
    }
    const canvas = root.document.createElement('canvas');
    canvas.width = width;
    canvas.height = height;
    return canvas;
  };

  function opacity(ageMs) {
    if (!Number.isFinite(ageMs) || ageMs < 0 || ageMs >= END_MS) return 0;
    return ageMs <= HOLD_MS ? 1 : (END_MS - ageMs) / (END_MS - HOLD_MS);
  }

  function setCanvasFactory(factory) {
    if (typeof factory !== 'function') throw new TypeError('Canvas factory must be a function.');
    canvasFactory = factory;
    sheets = new WeakMap();
    scratch = null;
  }

  function whiteSheet(source) {
    const width = source.naturalWidth || source.width;
    const height = source.naturalHeight || source.height;
    if (!(width > 0 && height > 0)) return null;
    const cached = sheets.get(source);
    if (cached && cached.width === width && cached.height === height) return cached.canvas;
    const canvas = canvasFactory(width, height), mask = canvas.getContext('2d');
    mask.imageSmoothingEnabled = false;
    mask.drawImage(source, 0, 0, width, height);
    mask.globalCompositeOperation = 'source-in';
    mask.fillStyle = '#ffffff';
    mask.fillRect(0, 0, width, height);
    mask.globalCompositeOperation = 'source-over';
    sheets.set(source, { canvas, width, height });
    return canvas;
  }

  // src = [sx, sy, sw, sh], dst = [dx, dy, dw, dh]. Coordinates match drawImage.
  // Drawing original + masked white directly onto the scene would increase the
  // alpha of translucent edge pixels. Tint RGB on an isolated frame instead.
  function drawRegion(ctx, source, src, dst, ageMs) {
    const amount = opacity(ageMs);
    ctx.save();
    try {
      ctx.imageSmoothingEnabled = false;
      if (amount === 0) {
        ctx.drawImage(source, ...src, ...dst);
      } else {
        const width = src[2], height = src[3];
        if (!scratch || scratch.width !== width || scratch.height !== height) {
          scratch = canvasFactory(width, height);
        }
        const tint = scratch.getContext('2d');
        tint.globalAlpha = 1;
        tint.globalCompositeOperation = 'source-over';
        tint.imageSmoothingEnabled = false;
        tint.clearRect(0, 0, width, height);
        // Isolate the source cell before scaling in both browser and offline
        // Canvas implementations; adjacent cells cannot bleed into this draw.
        tint.drawImage(amount === 1 ? whiteSheet(source) : source, ...src, 0, 0, width, height);
        if (amount < 1) {
          tint.globalCompositeOperation = 'source-atop';
          tint.globalAlpha = amount;
          tint.fillStyle = '#ffffff';
          tint.fillRect(0, 0, width, height);
        }
        tint.globalAlpha = 1;
        tint.globalCompositeOperation = 'source-over';
        ctx.drawImage(scratch, 0, 0, width, height, ...dst);
      }
    } finally {
      ctx.restore();
    }
    return amount;
  }

  // Same crop, rounded destination, scale, and pivot as motions-v6 drawClip.
  function drawClip(ctx, id, index, x = 500, foot = 590, height = 1.1, ageMs = -1) {
    const clip = PL_BY_ID[id], source = images['pl_' + id];
    if (!clip || !source) return null;
    const scale = height * 116.13 / PL_REF[clip.actor];
    const left = x - clip.pivot[0] * scale, top = foot - clip.pivot[1] * scale;
    drawRegion(ctx, source,
      [index % clip.columns * clip.frameWidth, Math.floor(index / clip.columns) * clip.frameHeight, clip.frameWidth, clip.frameHeight],
      [Math.round(left), Math.round(top), Math.round(clip.frameWidth * scale), Math.round(clip.frameHeight * scale)], ageMs);
    return { left, top, w: clip.frameWidth * scale, h: clip.frameHeight * scale, index };
  }

  // Same idle replacement / legacy-atlas geometry as the existing sprite helper.
  // Death dissolve and wireframe branches stay with their existing renderers.
  function drawSprite(ctx, name, x, foot, height, ageMs = -1) {
    if (name === 'on_00' && clipAvailable('hero-v5-idle')) {
      return drawClip(ctx, 'hero-v5-idle', 1, x, foot, 1.1, ageMs);
    }
    const frame = A[name];
    const idle = name.startsWith('gate_') ? A['gate_' + String(Number(name.slice(5)) % 5).padStart(2, '0')] : A.on_00;
    const h = height * 116.13 * frame.h / idle.h, w = h * frame.w / frame.h;
    const left = x - w * frame.px, top = foot - h;
    drawRegion(ctx, images[frame.sheet], [frame.x, frame.y, frame.w, frame.h], [px(left), px(top), px(w), px(h)], ageMs);
    return { left, top, w, h };
  }

  const api = Object.freeze({ holdMs: HOLD_MS, endMs: END_MS, opacity, whiteSheet, drawRegion, drawClip, drawSprite, setCanvasFactory });
  root.HitFlashV7 = api;
  if (root.window) root.window.HitFlashV7 = api;
})(globalThis);
