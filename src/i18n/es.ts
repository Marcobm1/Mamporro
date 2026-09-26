// Textos en español. Es el diccionario de referencia: sus claves definen el tipo
// `TranslationKey`, y el inglés debe tener exactamente las mismas.

export const es = {
  'app.title': 'MAMPORRO',
  'app.tagline': 'Supervivencia a chanclazo limpio',
  'app.loading': 'Calentando la chancla...',
  'app.prototype': 'Prototipo · Hito 1: moverse por el mapa',

  'error.webgl':
    'Tu navegador no puede ejecutar WebGL2. Prueba con Chrome, Edge o Firefox actualizados.',
  'error.pointerLock': 'No se pudo capturar el ratón. Espera un segundo y vuelve a hacer clic.',

  'title.play': 'Jugar',
  'title.clickHint': 'Al jugar se captura el ratón. Pulsa Esc para soltarlo.',
  'title.seedLabel': 'Semilla',
  'title.seedPlaceholder': 'Aleatoria',
  'title.newMap': 'Nuevo mapa',
  'title.currentSeed': 'Mapa actual: {seed}',

  'controls.title': 'Controles',
  'controls.move': 'Moverse',
  'controls.look': 'Mirar',
  'controls.jump': 'Saltar',
  'controls.slide': 'Deslizarse',
  'controls.pause': 'Pausa',
  'controls.debug': 'Panel de debug',
  'controls.keys.move': 'W A S D',
  'controls.keys.look': 'Ratón',
  'controls.keys.jump': 'Espacio',
  'controls.keys.slide': 'Shift / C',
  'controls.keys.slideCtrl': 'Shift / C / Ctrl',
  'controls.keys.pause': 'Esc',
  'controls.keys.debug': 'F3',

  'pause.title': 'Pausa',
  'pause.resume': 'Continuar',
  'pause.seed': 'Semilla: {seed}',
  'pause.backToTitle': 'Volver al inicio',

  'options.title': 'Opciones',
  'options.language': 'Idioma',
  'options.sensitivity': 'Sensibilidad del ratón',
  'options.resolution': 'Resolución interna',
  'options.resolutionValue': '{h} px',
  'options.vertexSnap': 'Temblor de vértices (PS1)',
  'options.dithering': 'Tramado de color (dithering)',
  'options.showFps': 'Mostrar FPS',
  'options.slideCtrl': 'Usar Ctrl para deslizarse',
  'options.slideCtrlWarning':
    '¡Cuidado! En Chrome y Edge, Ctrl+W cierra la pestaña y el juego no puede evitarlo.',

  'lang.es': 'Español',
  'lang.en': 'English',

  'hud.fps': '{fps} FPS',

  'character.remedios': 'Doña Remedios',
  'enemy.pelusa': 'Pelusa Rebelde',
  'enemy.cucaracha': 'Cucaracha Turbo',
  'weapon.chancla': 'Chancla Teledirigida',
  'weapon.chancla.desc': 'Busca al enemigo más cercano. Nunca falla. Casi nunca.',
  'weapon.naftalina': 'Eau de Naftalina',
  'weapon.naftalina.desc': 'Nube de olor a armario de abuela que daña a todo el que se acerque.',

  'debug.title': 'DEBUG · F3',
  'debug.fps': 'FPS',
  'debug.frame': 'Frame',
  'debug.logic': 'Lógica',
  'debug.render': 'Render',
  'debug.drawCalls': 'Draw calls',
  'debug.triangles': 'Triángulos',
  'debug.resolution': 'Resolución',
  'debug.position': 'Posición',
  'debug.speed': 'Velocidad',
  'debug.state': 'Estado',
  'debug.slope': 'Pendiente',
  'debug.seed': 'Semilla',
  'debug.state.grounded': 'suelo',
  'debug.state.airborne': 'aire',
  'debug.state.sliding': 'deslizando',
  'debug.state.steep': 'resbalando',
} as const;

export type TranslationKey = keyof typeof es;
