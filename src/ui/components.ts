// Componentes compartidos por varias pantallas: leyenda de controles y panel de opciones.
import { LANGUAGES, t, type Language, type TranslationKey } from '../i18n';
import {
  RENDER_HEIGHTS,
  SENSITIVITY_MAX,
  SENSITIVITY_MIN,
  type Settings,
} from '../save/schema';
import { h, segmented, toggle } from './dom';

const LANGUAGE_LABEL: Readonly<Record<Language, TranslationKey>> = {
  es: 'lang.es',
  en: 'lang.en',
};

export function languageSelector(current: Language, onChange: (lang: Language) => void): HTMLDivElement {
  return segmented(
    LANGUAGES.map((lang) => ({ value: lang, label: t(LANGUAGE_LABEL[lang]) })),
    current,
    onChange,
  );
}

export function controlsLegend(slideWithCtrl: boolean): HTMLDivElement {
  const entries: Array<[TranslationKey, TranslationKey]> = [
    ['controls.keys.move', 'controls.move'],
    ['controls.keys.look', 'controls.look'],
    ['controls.keys.jump', 'controls.jump'],
    [slideWithCtrl ? 'controls.keys.slideCtrl' : 'controls.keys.slide', 'controls.slide'],
    ['controls.keys.pause', 'controls.pause'],
    ['controls.keys.debug', 'controls.debug'],
  ];
  const grid = h('div', { className: 'keys' });
  for (const [keys, action] of entries) {
    grid.append(h('span', { className: 'kbd', text: t(keys) }), h('span', { text: t(action) }));
  }
  return h('div', { className: 'stack' }, h('div', { className: 'muted', text: t('controls.title') }), grid);
}

/**
 * Panel de opciones. Cada control actualiza su propio aspecto y avisa con
 * `onChange`; así un deslizador no se reconstruye mientras se arrastra.
 */
export function optionsPanel(settings: Settings, onChange: (patch: Partial<Settings>) => void): HTMLDivElement {
  const row = (label: string, control: Node): HTMLDivElement =>
    h('div', { className: 'row' }, h('span', { className: 'option-label', text: label }), control);

  // Sensibilidad del ratón
  const sensitivityValue = h('span', { text: settings.mouseSensitivity.toFixed(1) });
  const slider = h('input', {
    className: 'slider',
    attrs: {
      type: 'range',
      min: String(SENSITIVITY_MIN),
      max: String(SENSITIVITY_MAX),
      step: '0.1',
      value: String(settings.mouseSensitivity),
    },
  });
  slider.addEventListener('input', () => {
    const value = Number(slider.value);
    sensitivityValue.textContent = value.toFixed(1);
    onChange({ mouseSensitivity: value });
  });

  // Ctrl para deslizarse, con aviso visible mientras esté activado.
  const ctrlWarning = h('div', { className: 'warning', text: t('options.slideCtrlWarning') });
  ctrlWarning.hidden = !settings.slideWithCtrl;

  return h(
    'div',
    { className: 'stack' },
    h('div', { className: 'muted', text: t('options.title') }),
    row(t('options.language'), languageSelector(settings.language, (language) => onChange({ language }))),
    row(t('options.sensitivity'), h('div', { className: 'row' }, slider, sensitivityValue)),
    row(
      t('options.resolution'),
      segmented(
        RENDER_HEIGHTS.map((height) => ({ value: height, label: t('options.resolutionValue', { h: height }) })),
        settings.renderHeight,
        (renderHeight) => onChange({ renderHeight }),
      ),
    ),
    toggle(t('options.vertexSnap'), settings.vertexSnap, (vertexSnap) => onChange({ vertexSnap })),
    toggle(t('options.dithering'), settings.dithering, (dithering) => onChange({ dithering })),
    toggle(t('options.showFps'), settings.showFps, (showFps) => onChange({ showFps })),
    toggle(t('options.slideCtrl'), settings.slideWithCtrl, (slideWithCtrl) => {
      ctrlWarning.hidden = !slideWithCtrl;
      onChange({ slideWithCtrl });
    }),
    ctrlWarning,
  );
}
