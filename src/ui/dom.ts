// Pequeños ayudantes para construir la interfaz con DOM puro (sin frameworks).

export type Child = Node | string | null | undefined | false;

export interface ElementProps {
  className?: string;
  text?: string;
  attrs?: Readonly<Record<string, string>>;
  onClick?: (event: MouseEvent) => void;
}

export function h<K extends keyof HTMLElementTagNameMap>(
  tag: K,
  props: ElementProps = {},
  ...children: Child[]
): HTMLElementTagNameMap[K] {
  const el = document.createElement(tag);
  const base: HTMLElement = el;
  if (props.className) base.className = props.className;
  if (props.text !== undefined) base.textContent = props.text;
  if (props.attrs) for (const [name, value] of Object.entries(props.attrs)) base.setAttribute(name, value);
  if (props.onClick) base.addEventListener('click', props.onClick);
  for (const child of children) if (child) el.append(child);
  return el;
}

export function button(label: string, onClick: () => void, className = 'btn'): HTMLButtonElement {
  return h('button', { className, text: label, attrs: { type: 'button' }, onClick: () => onClick() });
}

/** Casilla de verificación pixelada; gestiona su propio estado visual. */
export function toggle(label: string, value: boolean, onChange: (value: boolean) => void): HTMLButtonElement {
  const el = h(
    'button',
    { className: 'toggle', attrs: { type: 'button', 'aria-pressed': String(value) } },
    h('span', { className: 'toggle__box' }),
    h('span', { text: label }),
  );
  el.addEventListener('click', () => {
    const next = el.getAttribute('aria-pressed') !== 'true';
    el.setAttribute('aria-pressed', String(next));
    onChange(next);
  });
  return el;
}

export interface SegmentOption<T> {
  value: T;
  label: string;
}

/** Selector de una opción entre varias (botones contiguos). */
export function segmented<T>(
  options: ReadonlyArray<SegmentOption<T>>,
  value: T,
  onChange: (value: T) => void,
): HTMLDivElement {
  const container = h('div', { className: 'seg' });
  const buttons = options.map((option) => {
    const b = h('button', {
      text: option.label,
      attrs: { type: 'button', 'aria-pressed': String(option.value === value) },
    });
    b.addEventListener('click', () => {
      buttons.forEach((other) => other.setAttribute('aria-pressed', String(other === b)));
      onChange(option.value);
    });
    return b;
  });
  container.append(...buttons);
  return container;
}

/** Ajusta el tamaño del "píxel de fuente" a un número entero de píxeles físicos. */
export function applyUiScale(root: HTMLElement): void {
  const dpr = window.devicePixelRatio || 1;
  const devicePixels = Math.max(1, Math.round((window.innerHeight * dpr) / 450));
  root.style.setProperty('--fp', `${devicePixels / dpr}px`);
}
