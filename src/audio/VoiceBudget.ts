import { AUDIO_CONFIG, SOUNDS, type SoundId } from '../data/audio';

/** Reservar voces para señales importantes evita que las hordas las tapen. */
export class VoiceBudget {
  private readonly active = new Map<number, { end: number; priority: number }>();
  private readonly last = new Map<SoundId, number>();
  private sequence = 0;
  claim(id: SoundId, now: number): number | null {
    for (const [key, voice] of this.active) if (voice.end <= now) this.active.delete(key);
    const def = SOUNDS[id];
    if (now - (this.last.get(id) ?? -Infinity) < def.cooldown) return null;
    if (this.active.size >= AUDIO_CONFIG.voices) return null;
    if (def.priority === 0 && this.active.size >= AUDIO_CONFIG.voices - AUDIO_CONFIG.reserved) return null;
    const key = ++this.sequence;
    this.active.set(key, { end: now + def.duration, priority: def.priority });
    this.last.set(id, now);
    return key;
  }
  release(key: number): void { this.active.delete(key); }
  clear(): void { this.active.clear(); this.last.clear(); }
  get count(): number { return this.active.size; }
}
