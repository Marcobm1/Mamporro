import { getLanguage, t } from '../i18n';
import { prepareProgressExport } from '../save/exportProgress';
import { button, h } from './dom';

export function downloadProgress(json: string): void {
  const blob = new Blob([json], { type: 'application/json;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  let anchor: HTMLAnchorElement | undefined;
  try {
    anchor = document.createElement('a');
    anchor.href = url; anchor.download = 'mamporro-progreso.json';
    document.body.append(anchor); anchor.click();
  } finally {
    anchor?.remove();
    // Dar tiempo al navegador para consumir el Blob; no se conserva una URL permanente.
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}

/** Solo en opciones del menú principal. La vista previa congela el JSON, no referencias al juego. */
export function progressExportPanel(read: () => unknown, canSave: () => boolean): HTMLElement {
  const report = h('div', { className: 'stack', attrs: { 'aria-live': 'polite' } });
  return h('div', { className: 'stack' },
    h('p', { className: 'muted', text: t('export.help') }),
    button(t('export.title'), () => {
      const result = prepareProgressExport(read, getLanguage());
      report.replaceChildren();
      if (!canSave()) report.append(h('p', { className: 'warning', text: t('export.memory') }));
      report.append(h('p', { text: t(result.json === null ? 'export.invalid' : 'export.preview') }));
      const preserved = h('details', {}, h('summary', { text: t('export.preserved') }));
      for (const issue of result.issues) {
        const line = h('p', { text: `${issue.path || '/'}: ${t(`export.reason.${issue.reason}`)}${issue.before === undefined ? '' : ` (${issue.before})`}${issue.after === undefined ? '' : ` → ${issue.after}`}` });
        if (issue.reason === 'preserve') preserved.append(line); else report.append(line);
      }
      report.append(preserved);
      const json = result.json;
      if (json !== null) report.append(button(t('export.download'), () => {
        try { downloadProgress(json); report.append(h('p', { text: t('export.started') })); }
        catch { report.append(h('p', { className: 'warning', text: t('export.failed') })); }
      }));
    }), report);
}
