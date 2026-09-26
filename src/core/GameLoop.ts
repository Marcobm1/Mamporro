// Bucle principal: lógica a paso fijo (60 Hz) y render interpolado a la tasa de refresco.

export interface LoopCallbacks {
  /** Avanza la simulación exactamente `dt` segundos. */
  update(dt: number): void;
  /**
   * Dibuja un frame. `alpha` (0..1) indica cuánto se ha avanzado hacia el
   * siguiente paso de lógica, para interpolar posiciones.
   */
  render(alpha: number, frameDt: number): void;
}

export class GameLoop {
  static readonly STEP = 1 / 60;
  /** Máximo de pasos de lógica por frame para evitar la "espiral de la muerte". */
  private static readonly MAX_STEPS = 6;
  /** Un frame nunca cuenta más de esto (p. ej. al volver de otra pestaña). */
  private static readonly MAX_FRAME_TIME = 0.25;

  private accumulator = 0;
  private lastTime = -1;
  private rafId = 0;
  private running = false;

  // Métricas para el panel de debug.
  frameMs = 0;
  updateMs = 0;
  renderMs = 0;
  ticksThisFrame = 0;

  constructor(private readonly callbacks: LoopCallbacks) {}

  start(): void {
    if (this.running) return;
    this.running = true;
    this.lastTime = -1;
    this.rafId = requestAnimationFrame(this.frame);
  }

  stop(): void {
    this.running = false;
    cancelAnimationFrame(this.rafId);
  }

  /** Descarta el tiempo acumulado (útil al salir de una pausa). */
  resetAccumulator(): void {
    this.accumulator = 0;
    this.lastTime = -1;
  }

  private readonly frame = (now: number): void => {
    if (!this.running) return;
    this.rafId = requestAnimationFrame(this.frame);

    if (this.lastTime < 0) this.lastTime = now;
    let frameDt = (now - this.lastTime) / 1000;
    this.lastTime = now;
    if (frameDt > GameLoop.MAX_FRAME_TIME) frameDt = GameLoop.MAX_FRAME_TIME;
    this.frameMs = frameDt * 1000;

    const t0 = performance.now();
    this.accumulator += frameDt;
    let steps = 0;
    while (this.accumulator >= GameLoop.STEP && steps < GameLoop.MAX_STEPS) {
      this.callbacks.update(GameLoop.STEP);
      this.accumulator -= GameLoop.STEP;
      steps++;
    }
    // Si vamos muy atrasados, descartamos el resto en vez de acumular retraso.
    if (steps === GameLoop.MAX_STEPS) this.accumulator = 0;
    this.ticksThisFrame = steps;
    const t1 = performance.now();

    this.callbacks.render(this.accumulator / GameLoop.STEP, frameDt);
    const t2 = performance.now();

    this.updateMs = t1 - t0;
    this.renderMs = t2 - t1;
  };
}
