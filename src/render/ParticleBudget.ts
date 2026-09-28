/** Presupuesto decorativo: no se aplica a proyectiles ni a avisos de ataques. */
export class ParticleBudget {
  private remaining = 256;
  reduced = false;
  get perFrame(): number { return this.reduced ? 64 : 256; }
  get capacity(): number { return this.reduced ? 400 : 1500; }
  reset(): void { this.remaining = this.perFrame; }
  take(requested: number, active: number): number {
    const wanted = this.reduced ? Math.ceil(requested * 0.25) : requested;
    const granted = Math.max(0, Math.min(wanted, this.remaining, this.capacity - active));
    this.remaining -= granted;
    return granted;
  }
}
