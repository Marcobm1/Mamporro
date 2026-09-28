import { describe, expect, it } from 'vitest';
import { AUDIO_CONFIG, SOUNDS, type SoundId } from '../data/audio';
import { synthMusic, synthSound } from './synth';
import { VoiceBudget } from './VoiceBudget';

describe('audio procedural', () => {
  it('genera todos los timbres finitos, audibles, acotados y con bordes suaves', () => {
    for (const def of Object.values(SOUNDS)) {
      const samples = synthSound(def);
      expect(samples.length).toBe(Math.ceil(def.duration * AUDIO_CONFIG.sampleRate));
      expect(Math.abs(samples[0] ?? 1)).toBe(0);
      expect(Math.abs(samples[samples.length - 1] ?? 1)).toBe(0);
      let energy = 0;
      for (const sample of samples) { if (!Number.isFinite(sample) || Math.abs(sample) > 0.25) throw new Error('PCM inválido'); energy += sample * sample; }
      expect(energy).toBeGreaterThan(0.01);
      expect(samples).toEqual(synthSound(def));
    }
  });
  it('produce dos arreglos originales repetibles, de igual duración y sin saturación', () => {
    const normal = synthMusic(false);
    const intense = synthMusic(true);
    expect(normal.length).toBe(intense.length);
    expect(normal).not.toEqual(intense);
    for (const samples of [normal, intense]) {
      let peak = 0;
      for (const value of samples) { if (!Number.isFinite(value)) throw new Error('PCM no finito'); peak = Math.max(peak, Math.abs(value)); }
      expect(peak).toBeGreaterThan(0.1);
      expect(peak).toBeLessThan(0.5);
      expect(Math.abs(samples[0] ?? 1)).toBe(0);
      expect(Math.abs(samples[samples.length - 1] ?? 1)).toBe(0);
    }
    expect(normal).toEqual(synthMusic(false));
  });
  it('agrupa golpes repetidos y libera voces al terminar', () => {
    const budget = new VoiceBudget();
    const key = budget.claim('hit', 0);
    expect(key).not.toBeNull();
    for (let i = 0; i < 1000; i++) expect(budget.claim('hit', 0)).toBeNull();
    expect(budget.claim('hit', 1)).not.toBeNull();
    expect(budget.count).toBe(1);
    budget.clear();
    expect(budget.count).toBe(0);
    const next = budget.claim('hit', 1);
    if (next === null) throw new Error('Debe aceptar tras limpiar');
    budget.release(next);
    expect(budget.count).toBe(0);
  });
  it('reserva cuatro voces para avisos y nunca supera dieciséis', () => {
    const budget = new VoiceBudget();
    for (const id of Object.keys(SOUNDS) as SoundId[]) if (SOUNDS[id].priority === 0) budget.claim(id, 0);
    expect(budget.count).toBe(12);
    for (const id of ['hurt', 'level', 'shield', 'boss'] as const) expect(budget.claim(id, 0)).not.toBeNull();
    expect(budget.count).toBe(16);
    expect(budget.claim('victory', 0)).toBeNull();
    expect(budget.claim('victory', 3)).not.toBeNull();
    expect(budget.count).toBe(1);
  });
});
