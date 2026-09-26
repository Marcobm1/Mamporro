// Punto de entrada: crea el juego o, si el navegador no puede, muestra un aviso.
import './styles/main.css';
import { Game } from './core/Game';
import { detectLanguage, setLanguage, t } from './i18n';

function showFatal(message: string): void {
  const el = document.createElement('div');
  el.className = 'fatal';
  el.textContent = message;
  document.body.append(el);
}

const canvas = document.getElementById('game');
const uiRoot = document.getElementById('ui');

if (!(canvas instanceof HTMLCanvasElement) || !(uiRoot instanceof HTMLElement)) {
  throw new Error('index.html debe contener #game (canvas) y #ui');
}

const testMode = new URLSearchParams(window.location.search).has('test');

try {
  const game = new Game(canvas, uiRoot, { testMode });
  void game.boot();
} catch (err) {
  console.error(err);
  setLanguage(detectLanguage(navigator.languages ?? [navigator.language]));
  showFatal(t('error.webgl'));
}
