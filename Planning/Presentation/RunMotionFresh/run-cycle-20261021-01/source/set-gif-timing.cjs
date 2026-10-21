const fs = require('node:fs');
const path = require('node:path');
const file = path.join(__dirname, '..', 'run-preview.gif');
const data = fs.readFileSync(file);
const delays = [8, 6, 6, 8, 8, 6, 6, 8];
let position = 13 + ((data[10] & 128) ? 3 * (2 ** ((data[10] & 7) + 1)) : 0);
const header = Buffer.from(data.subarray(0, position));
header.write('GIF89a', 0, 'ascii');
const loop = Buffer.concat([Buffer.from([0x21, 0xff, 11]), Buffer.from('NETSCAPE2.0'), Buffer.from([3, 1, 0, 0, 0])]);
const result = [header, loop];
let frame = 0;
function skipBlocks() {
  while (data[position] !== 0) position += 1 + data[position];
  position++;
}
while (position < data.length) {
  const start = position;
  const marker = data[position++];
  if (marker === 0x3b) { result.push(Buffer.from([0x3b])); break; }
  if (marker === 0x21) {
    const label = data[position++];
    skipBlocks();
    if (label !== 0xf9 && label !== 0xff) result.push(data.subarray(start, position));
  } else if (marker === 0x2c) {
    const packed = data[position + 8];
    position += 9;
    if (packed & 128) position += 3 * (2 ** ((packed & 7) + 1));
    position++;
    skipBlocks();
    result.push(Buffer.from([0x21, 0xf9, 4, 8, delays[frame++], 0, 0, 0]));
    result.push(data.subarray(start, position));
  } else throw new Error(`Unexpected GIF block ${marker} at ${start}`);
}
if (frame !== 8) throw new Error(`Expected 8 frames, found ${frame}`);
fs.writeFileSync(file, Buffer.concat(result));
console.log('GIF: 8 frames, 560 ms, infinite loop.');
