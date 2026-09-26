// English texts. The `Record<TranslationKey, string>` type makes the compiler
// fail if any key from the Spanish dictionary is missing here.
import type { TranslationKey } from './es';

export const en: Record<TranslationKey, string> = {
  'app.title': 'MAMPORRO',
  'app.tagline': 'Survival, one flying slipper at a time',
  'app.loading': 'Warming up the slipper...',
  'app.prototype': 'Prototype · Milestone 1: moving around the map',

  'error.webgl': 'Your browser cannot run WebGL2. Try an up-to-date Chrome, Edge or Firefox.',
  'error.pointerLock': 'Could not capture the mouse. Wait a second and click again.',

  'title.play': 'Play',
  'title.clickHint': 'Playing captures the mouse. Press Esc to release it.',
  'title.seedLabel': 'Seed',
  'title.seedPlaceholder': 'Random',
  'title.newMap': 'New map',
  'title.currentSeed': 'Current map: {seed}',

  'controls.title': 'Controls',
  'controls.move': 'Move',
  'controls.look': 'Look',
  'controls.jump': 'Jump',
  'controls.slide': 'Slide',
  'controls.pause': 'Pause',
  'controls.debug': 'Debug panel',
  'controls.keys.move': 'W A S D',
  'controls.keys.look': 'Mouse',
  'controls.keys.jump': 'Space',
  'controls.keys.slide': 'Shift / C',
  'controls.keys.slideCtrl': 'Shift / C / Ctrl',
  'controls.keys.pause': 'Esc',
  'controls.keys.debug': 'F3',

  'pause.title': 'Paused',
  'pause.resume': 'Resume',
  'pause.seed': 'Seed: {seed}',
  'pause.backToTitle': 'Back to title',

  'options.title': 'Options',
  'options.language': 'Language',
  'options.sensitivity': 'Mouse sensitivity',
  'options.resolution': 'Internal resolution',
  'options.resolutionValue': '{h} px',
  'options.vertexSnap': 'Vertex jitter (PS1)',
  'options.dithering': 'Color dithering',
  'options.showFps': 'Show FPS',
  'options.slideCtrl': 'Use Ctrl to slide',
  'options.slideCtrlWarning':
    'Careful! In Chrome and Edge, Ctrl+W closes the tab and the game cannot prevent it.',

  'lang.es': 'Español',
  'lang.en': 'English',

  'hud.fps': '{fps} FPS',

  'debug.title': 'DEBUG · F3',
  'debug.fps': 'FPS',
  'debug.frame': 'Frame',
  'debug.logic': 'Logic',
  'debug.render': 'Render',
  'debug.drawCalls': 'Draw calls',
  'debug.triangles': 'Triangles',
  'debug.resolution': 'Resolution',
  'debug.position': 'Position',
  'debug.speed': 'Speed',
  'debug.state': 'State',
  'debug.slope': 'Slope',
  'debug.seed': 'Seed',
  'debug.state.grounded': 'ground',
  'debug.state.airborne': 'air',
  'debug.state.sliding': 'sliding',
  'debug.state.steep': 'slipping',
};
