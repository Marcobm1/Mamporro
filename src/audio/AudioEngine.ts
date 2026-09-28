import { AUDIO_CONFIG, SOUNDS, type SoundId } from '../data/audio';
import type { Settings } from '../save/schema';
import { synthMusic, synthSound } from './synth';
import { VoiceBudget } from './VoiceBudget';

export type MusicMode = 'menu' | 'playing' | 'intense' | 'paused' | 'results';
/** Un contexto, buffers reutilizables y un máximo de 16 efectos + una pista. */
export class AudioEngine {
  private context: AudioContext | null = null;
  private effects: GainNode | null = null;
  private music: GainNode | null = null;
  private master: GainNode | null = null;
  private readonly buffers = new Map<SoundId, AudioBuffer>();
  private readonly tracks: AudioBuffer[] = [];
  private readonly voices = new Map<number, AudioBufferSourceNode>();
  private readonly budget = new VoiceBudget();
  private track: AudioBufferSourceNode | null = null;
  private trackIndex = -1;
  private mode: MusicMode = 'menu';
  private hidden = false;
  private settings: Pick<Settings, 'musicVolume' | 'effectsVolume' | 'muted'> = { musicVolume: 0.5, effectsVolume: 0.7, muted: false };

  /** Solo se invoca desde gestos del usuario; nunca al cargar la página. */
  unlock(): void {
    if (this.hidden || typeof AudioContext === 'undefined') return;
    try {
      if (!this.context) this.create();
      void this.context?.resume().then(() => {
        if (this.hidden) { void this.context?.suspend().catch(() => {}); return; }
        this.applyLevels();
        this.updateTrack();
      }).catch(() => {});
    } catch { /* Sin audio disponible, el juego sigue siendo utilizable. */ }
  }

  private create(): void {
    const ctx = new AudioContext();
    this.context = ctx;
    this.effects = ctx.createGain();
    this.music = ctx.createGain();
    this.master = ctx.createGain();
    const compressor = ctx.createDynamicsCompressor();
    compressor.threshold.value = -12;
    compressor.ratio.value = 8;
    this.effects.connect(this.master);
    this.music.connect(this.master);
    this.master.connect(compressor);
    compressor.connect(ctx.destination);
    const buffer = (data: Float32Array): AudioBuffer => {
      const result = ctx.createBuffer(1, data.length, AUDIO_CONFIG.sampleRate);
      result.getChannelData(0).set(data);
      return result;
    };
    for (const id of Object.keys(SOUNDS) as SoundId[]) this.buffers.set(id, buffer(synthSound(SOUNDS[id])));
    this.tracks.push(buffer(synthMusic(false)), buffer(synthMusic(true)));
    this.applyLevels();
  }

  configure(settings: Pick<Settings, 'musicVolume' | 'effectsVolume' | 'muted'>): void {
    this.settings = { ...settings };
    if (settings.muted || settings.effectsVolume === 0) this.clearEffects();
    this.applyLevels();
    this.updateTrack();
  }

  setMode(mode: MusicMode): void {
    if (mode === this.mode) return;
    this.mode = mode;
    this.applyLevels();
    this.updateTrack();
  }

  setHidden(hidden: boolean): void {
    this.hidden = hidden;
    if (hidden) {
      this.clearEffects();
      void this.context?.suspend().catch(() => {});
    } else if (this.context) this.unlock();
  }

  play(id: SoundId): void {
    const ctx = this.context;
    if (!ctx || ctx.state !== 'running' || this.hidden || this.settings.muted || this.settings.effectsVolume === 0 || !this.effects) return;
    // El reloj de audio avanza aunque JS esté ocupado: onended puede llegar tarde.
    // Limitar también los nodos pendientes evita acumularlos durante una ráfaga.
    if (this.voices.size >= AUDIO_CONFIG.voices) return;
    const key = this.budget.claim(id, ctx.currentTime);
    if (key === null) return;
    const source = ctx.createBufferSource();
    source.buffer = this.buffers.get(id) ?? null;
    source.connect(this.effects);
    this.voices.set(key, source);
    source.onended = () => { source.disconnect(); this.voices.delete(key); this.budget.release(key); };
    source.start();
  }

  clearEffects(): void {
    for (const source of this.voices.values()) { source.stop(); source.disconnect(); }
    this.voices.clear();
    this.budget.clear();
  }

  private applyLevels(): void {
    const ctx = this.context;
    if (!ctx || !this.master || !this.effects || !this.music) return;
    this.master.gain.setTargetAtTime(this.settings.muted ? 0 : 0.8, ctx.currentTime, 0.015);
    this.effects.gain.setTargetAtTime(this.settings.effectsVolume, ctx.currentTime, 0.015);
    this.music.gain.setTargetAtTime(this.settings.musicVolume * (this.mode === 'paused' ? 0.25 : this.mode === 'menu' ? 0.65 : 1), ctx.currentTime, 0.04);
  }

  private updateTrack(): void {
    const ctx = this.context;
    if (!ctx || ctx.state !== 'running' || !this.music) return;
    const index = this.mode === 'intense' ? 1 : 0;
    if (this.settings.muted || this.settings.musicVolume === 0) {
      this.track?.stop(); this.track?.disconnect(); this.track = null; this.trackIndex = -1;
      return;
    }
    if (index === this.trackIndex) return;
    this.track?.stop(); this.track?.disconnect();
    const source = ctx.createBufferSource();
    source.buffer = this.tracks[index] ?? null;
    source.loop = true;
    source.connect(this.music);
    // Ambas variantes comparten compás; conservar la posición al cambiar.
    const duration = source.buffer?.duration ?? 1;
    source.start(0, ctx.currentTime % duration);
    this.track = source;
    this.trackIndex = index;
  }

  get stats(): { state: string; voices: number; mode: MusicMode; tracks: number } {
    return { state: this.context?.state ?? 'locked', voices: this.voices.size, mode: this.mode, tracks: this.track ? 1 : 0 };
  }
}
