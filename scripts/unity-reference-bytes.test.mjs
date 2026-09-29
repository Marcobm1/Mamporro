import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { test } from 'node:test';
import { referenceBytes } from './unity-reference-bytes.mjs';

const hash = (file, bytes) => createHash('sha256').update(referenceBytes(file, bytes)).digest('hex');
const texts = ['src/core/rng.ts', 'src/types/opentype.d.ts', 'src/styles/main.css',
  'package-lock.json', 'unity/Docs/Reference/baseline.json', 'unity/Docs/Reference/report.json'];

test('LF y CRLF coinciden en cada clase de texto autorizada', () => {
  const lf = Buffer.from('{\n  "texto": "España",\n  "valor": 1\n}\n');
  const crlf = Buffer.from(lf.toString().replaceAll('\n', '\r\n'));
  for (const file of texts) {
    assert.deepEqual(referenceBytes(file, crlf), lf);
    assert.equal(hash(file, crlf), hash(file, lf));
  }
});

test('los cambios reales de contenido siguen fallando, incluso en JSON', () => {
  for (const file of texts) {
    const original = Buffer.from('{\n  "valor": 1\n}\n');
    const changed = Buffer.from('{\r\n  "valor": 2\r\n}\r\n');
    assert.throws(() => assert.equal(hash(file, changed), hash(file, original)),
      { code: 'ERR_ASSERTION' });
  }
});

test('espacios, formato JSON, CR aislado y bytes no UTF-8 se conservan', () => {
  const raw = Buffer.from([0xef, 0xbb, 0xbf, 0xff, 32, 9, 13, 65, 13, 10, 32]);
  const wanted = Buffer.from([0xef, 0xbb, 0xbf, 0xff, 32, 9, 13, 65, 10, 32]);
  for (const file of texts) {
    assert.deepEqual(referenceBytes(file, raw), wanted);
    assert.notEqual(hash(file, Buffer.from('{"x":1}\n')), hash(file, Buffer.from('{ "x": 1 }\n')));
    assert.notEqual(hash(file, Buffer.from('x\n')), hash(file, Buffer.from('x \n')));
  }
});

test('PNG, WAV y archivos no clasificados conservan CRLF y detectan alteraciones', () => {
  const raw = Buffer.from([0x89, 13, 10, 0, 255, 65]);
  for (const file of ['unity/Docs/Reference/image.png', 'unity/Docs/Reference/music.wav',
    'unity/Docs/Reference/media.json', 'src/data/unknown.json', 'src/data/unknown.bin', 'other.ts']) {
    assert.deepEqual(referenceBytes(file, raw), raw);
    const changed = Buffer.from(raw);
    changed[1] ^= 1;
    assert.throws(() => assert.equal(hash(file, changed), hash(file, raw)),
      { code: 'ERR_ASSERTION' });
    const withoutCR = Buffer.from([0x89, 10, 0, 255, 65]);
    assert.notEqual(hash(file, raw), hash(file, withoutCR));
  }
});
