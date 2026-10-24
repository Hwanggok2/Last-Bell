const fs = require('fs');
const path = require('path');
const { createCanvas, loadImage } = require('C:/Users/JW/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@napi-rs/canvas');

async function main() {
  const base = path.resolve(__dirname, '..');
  const npc = await loadImage(path.resolve(base, '../../images/actors-pixel-v3.png'));
  const originals = [
    [path.resolve(base, '../Player-v2/canonical-coarse.png'), '현재 루카', '원본 32px · 8색'],
    [path.join(base, 'canonical-a-outline.png'), 'A · 외곽 정리', '얼굴·몸·발 위치 유지'],
    [path.join(base, 'canonical-b-head.png'), 'B · 머리 1px 축소', '얼굴·손·검·발 위치 유지']
  ];
  const c = createCanvas(1536, 1024), ctx = c.getContext('2d');
  ctx.imageSmoothingEnabled = false;
  ctx.fillStyle = '#FFF3DA'; ctx.fillRect(0, 0, c.width, c.height);
  ctx.fillStyle = '#20333C'; ctx.font = 'bold 28px "Malgun Gothic"';
  ctx.fillText('NPC 그림체와 루카 원형 비교', 52, 46);
  ctx.drawImage(npc, 430, 160, 1742, 510, 112, 76, 1310, 384);
  ctx.font = '20px "Malgun Gothic"'; ctx.fillText('토리 · 리나 · 바룬 · 도토 / 기존 NPC 컨셉의 해당 부분', 52, 494);
  for (let i = 0; i < originals.length; i++) {
    const [file, title, note] = originals[i], x = 80 + i * 488;
    ctx.fillStyle = '#DBE9D5'; ctx.fillRect(x, 540, 400, 360);
    ctx.strokeStyle = '#8F9D88'; ctx.lineWidth = 1;
    ctx.beginPath(); ctx.moveTo(x, 850.5); ctx.lineTo(x + 400, 850.5); ctx.stroke();
    const im = await loadImage(file); ctx.drawImage(im, x + 40, 560, 320, 320);
    ctx.fillStyle = '#20333C'; ctx.font = 'bold 25px "Malgun Gothic"'; ctx.fillText(title, x, 938);
    ctx.font = '20px "Malgun Gothic"'; ctx.fillText(note, x, 976);
  }
  fs.writeFileSync(path.join(__dirname, 'canonical-comparison.png'), c.toBuffer('image/png'));
}
main().catch(e => { console.error(e); process.exitCode = 1; });
