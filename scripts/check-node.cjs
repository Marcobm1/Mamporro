/*
 * Comprueba que la versión de Node.js sea compatible con el proyecto (>= 22.12.0).
 *
 * Está escrito a propósito en JavaScript "antiguo" (ES5, CommonJS) para que se
 * ejecute y muestre el aviso incluso con versiones muy viejas de Node.
 * Se lanza automáticamente antes de `npm install`, `npm run dev`, `build`,
 * `preview` y `test` (ver "scripts" en package.json).
 */
'use strict';

var REQUIRED = [22, 12, 0];
var version = process.versions.node;
var current = version.split('.').map(function (n) {
  return parseInt(n, 10) || 0;
});

function isLower(a, b) {
  for (var i = 0; i < 3; i++) {
    if (a[i] !== b[i]) return a[i] < b[i];
  }
  return false;
}

if (isLower(current, REQUIRED)) {
  console.error(
    [
      '',
      '====================================================================',
      '  MAMPORRO necesita Node.js 22.12 o superior.',
      '  Tu versión actual es: v' + version,
      '',
      '  Cómo actualizar (elige una opción):',
      '    - nvm (macOS / Linux):   nvm install   y después   nvm use',
      '    - nvm-windows:           nvm install lts   y después   nvm use lts',
      '    - Instalador oficial:    https://nodejs.org  (descarga la versión LTS)',
      '',
      '  Tienes los pasos detallados en el README, sección "Requisitos".',
      '  --------------------------------------------------------------',
      '  MAMPORRO requires Node.js 22.12 or newer (you have v' + version + ').',
      '====================================================================',
      ''
    ].join('\n')
  );
  process.exit(1);
}

// Vitest 5 solo da soporte oficial a las ramas 22, 24 y 26+ (las impares no son LTS).
if (current[0] === 23 || current[0] === 25) {
  console.warn(
    '[MAMPORRO] Aviso: Node v' + version + ' no es una versión LTS; los tests podrían fallar. ' +
      'Si ocurre, instala la LTS (ver README).'
  );
}
