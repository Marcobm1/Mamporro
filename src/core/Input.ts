// Entrada de teclado y ratón. Usa `event.code` (tecla física), así WASD funciona
// igual en teclados QWERTY, AZERTY, etc.

export type Action =
  | 'forward'
  | 'back'
  | 'left'
  | 'right'
  | 'jump'
  | 'slide'
  | 'interact'
  | 'debug';

const BASE_BINDINGS: Readonly<Record<Action, readonly string[]>> = {
  forward: ['KeyW', 'ArrowUp'],
  back: ['KeyS', 'ArrowDown'],
  left: ['KeyA', 'ArrowLeft'],
  right: ['KeyD', 'ArrowRight'],
  jump: ['Space'],
  slide: ['ShiftLeft', 'ShiftRight', 'KeyC'],
  interact: ['KeyE'],
  debug: ['F3'],
};

const CTRL_KEYS = ['ControlLeft', 'ControlRight'] as const;

/** Teclas cuyo comportamiento por defecto del navegador bloqueamos durante el juego. */
const PREVENT_DEFAULT = new Set([
  'Space',
  'ArrowUp',
  'ArrowDown',
  'ArrowLeft',
  'ArrowRight',
  'F3',
  'Tab',
]);

/** Límite por evento del movimiento del ratón: filtra picos erróneos de algunos navegadores. */
const MAX_MOUSE_DELTA = 300;

type LockListener = (locked: boolean) => void;
type KeyListener = (code: string) => void;

export class Input {
  private readonly down = new Set<string>();
  private readonly pressed = new Set<string>();
  private bindings: Record<Action, readonly string[]> = { ...BASE_BINDINGS };
  private mouseDX = 0;
  private mouseDY = 0;
  private locked = false;
  /** Permite mirar con el ratón sin Pointer Lock (solo para pruebas automáticas). */
  freeLook = false;
  private readonly lockListeners = new Set<LockListener>();
  private readonly lockErrorListeners = new Set<() => void>();
  private readonly keyListeners = new Set<KeyListener>();

  constructor(private readonly lockTarget: HTMLElement) {
    window.addEventListener('keydown', this.onKeyDown);
    window.addEventListener('keyup', this.onKeyUp);
    window.addEventListener('blur', this.onBlur);
    document.addEventListener('mousemove', this.onMouseMove);
    document.addEventListener('pointerlockchange', this.onLockChange);
    document.addEventListener('pointerlockerror', this.onLockError);
  }

  /** Activa o desactiva Ctrl como tecla de deslizarse (opción con aviso por Ctrl+W). */
  setSlideWithCtrl(enabled: boolean): void {
    const slide = BASE_BINDINGS.slide.filter((k) => !(CTRL_KEYS as readonly string[]).includes(k));
    this.bindings = {
      ...this.bindings,
      slide: enabled ? [...slide, ...CTRL_KEYS] : slide,
    };
  }

  isDown(action: Action): boolean {
    for (const code of this.bindings[action]) if (this.down.has(code)) return true;
    return false;
  }

  /** ¿Se pulsó la acción desde el último tick de lógica? */
  wasPressed(action: Action): boolean {
    for (const code of this.bindings[action]) if (this.pressed.has(code)) return true;
    return false;
  }

  /** Se llama al final de cada tick de lógica para limpiar las pulsaciones. */
  endTick(): void {
    this.pressed.clear();
  }

  /** Devuelve y reinicia el movimiento de ratón acumulado desde la última llamada. */
  consumeMouse(out: { dx: number; dy: number }): void {
    out.dx = this.mouseDX;
    out.dy = this.mouseDY;
    this.mouseDX = 0;
    this.mouseDY = 0;
  }

  get pointerLocked(): boolean {
    return this.locked;
  }

  async requestPointerLock(): Promise<void> {
    const el = this.lockTarget;
    try {
      // `unadjustedMovement` da movimiento "crudo" (sin aceleración del SO) donde se soporta.
      await el.requestPointerLock({ unadjustedMovement: true });
    } catch (err) {
      if (err instanceof DOMException && err.name === 'NotSupportedError') {
        try {
          await el.requestPointerLock();
        } catch {
          // El evento `pointerlockerror` ya avisa a quien escuche.
        }
      }
    }
  }

  exitPointerLock(): void {
    if (document.pointerLockElement) document.exitPointerLock();
  }

  onPointerLockChange(listener: LockListener): () => void {
    this.lockListeners.add(listener);
    return () => this.lockListeners.delete(listener);
  }

  onPointerLockError(listener: () => void): () => void {
    this.lockErrorListeners.add(listener);
    return () => this.lockErrorListeners.delete(listener);
  }

  /** Escucha pulsaciones de teclas (sin repetición), para atajos de interfaz. */
  onKey(listener: KeyListener): () => void {
    this.keyListeners.add(listener);
    return () => this.keyListeners.delete(listener);
  }

  /** Simula pulsaciones (usado por las pruebas automáticas). */
  simulateKey(code: string, isDown: boolean): void {
    if (isDown) {
      if (!this.down.has(code)) this.pressed.add(code);
      this.down.add(code);
    } else {
      this.down.delete(code);
    }
  }

  private isTypingTarget(target: EventTarget | null): boolean {
    return (
      target instanceof HTMLInputElement ||
      target instanceof HTMLTextAreaElement ||
      target instanceof HTMLSelectElement
    );
  }

  private readonly onKeyDown = (e: KeyboardEvent): void => {
    if (this.isTypingTarget(e.target)) return;
    if (PREVENT_DEFAULT.has(e.code)) e.preventDefault();
    if (!e.repeat) {
      this.pressed.add(e.code);
      this.keyListeners.forEach((l) => l(e.code));
    }
    this.down.add(e.code);
  };

  private readonly onKeyUp = (e: KeyboardEvent): void => {
    this.down.delete(e.code);
  };

  private readonly onBlur = (): void => {
    // Evita teclas "pegadas" si la ventana pierde el foco con algo pulsado.
    this.down.clear();
    this.pressed.clear();
  };

  private readonly onMouseMove = (e: MouseEvent): void => {
    if (!this.locked && !this.freeLook) return;
    this.mouseDX += Math.max(-MAX_MOUSE_DELTA, Math.min(MAX_MOUSE_DELTA, e.movementX));
    this.mouseDY += Math.max(-MAX_MOUSE_DELTA, Math.min(MAX_MOUSE_DELTA, e.movementY));
  };

  private readonly onLockChange = (): void => {
    this.locked = document.pointerLockElement === this.lockTarget;
    this.mouseDX = 0;
    this.mouseDY = 0;
    this.lockListeners.forEach((l) => l(this.locked));
  };

  private readonly onLockError = (): void => {
    this.lockErrorListeners.forEach((l) => l());
  };
}
