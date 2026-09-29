// Solo estos textos de referencia admiten equivalencia entre LF y CRLF.
const textFiles = new Set([
  'package-lock.json',
  'unity/Docs/Reference/baseline.json',
  'unity/Docs/Reference/report.json',
]);

export function referenceBytes(file, bytes) {
  if (!textFiles.has(file) && !/^src\/.+\.(?:ts|css)$/.test(file)) return bytes;
  // No decodificar: conservar todos los bytes salvo CR inmediatamente antes de LF.
  const result = Buffer.allocUnsafe(bytes.length);
  let length = 0;
  for (let i = 0; i < bytes.length; i++) {
    if (bytes[i] === 13 && bytes[i + 1] === 10) continue;
    result[length++] = bytes[i];
  }
  return result.subarray(0, length);
}
