import { Rng } from '../core/rng';
import { AUDIO_CONFIG, BASS, MELODY, type SoundDef } from '../data/audio';

/** PCM mono puro: permite comprobar los sonidos sin navegador ni dispositivo. */
export function synthSound(def: SoundDef, sampleRate = AUDIO_CONFIG.sampleRate): Float32Array {
  const samples = new Float32Array(Math.ceil(def.duration * sampleRate));
  const rng = new Rng('sonido');
  let phase = 0;
  for (let i = 0; i < samples.length; i++) {
    const progress = i / (samples.length - 1);
    phase += (def.frequency + (def.end - def.frequency) * progress) / sampleRate;
    const wave = Math.sin(phase * Math.PI * 2) >= 0 ? 1 : -1;
    const envelope = Math.min(1, i / (sampleRate * 0.004)) * (1 - progress) ** 2;
    samples[i] = (wave * (1 - def.noise) + (rng.next() * 2 - 1) * def.noise) * def.gain * envelope;
  }
  return samples;
}

const hz = (midi: number): number => 440 * 2 ** ((midi - 69) / 12);
/** Bucle de ocho compases; dos arreglos de igual duración permiten cambiar de intensidad. */
export function synthMusic(intense: boolean, sampleRate = AUDIO_CONFIG.sampleRate): Float32Array {
  const beat = 60 / AUDIO_CONFIG.bpm;
  const samples = new Float32Array(Math.round(beat * 32 * sampleRate));
  const rng = new Rng('musica');
  for (let i = 0; i < samples.length; i++) {
    const time = i / sampleRate;
    const step = Math.floor(time / (beat / 2));
    const local = time % (beat / 2);
    const note = MELODY[step % MELODY.length] ?? -1;
    const bass = BASS[Math.floor(time / (beat * 4)) % BASS.length] ?? 48;
    const envelope = Math.min(1, local / 0.008) * Math.max(0, 1 - local / (beat * 0.4));
    const lead = note < 0 ? 0 : (Math.sin(hz(note) * time * Math.PI * 2) > 0 ? 1 : -1) * 0.065 * envelope;
    const low = Math.sin(hz(bass + (step % 4 >= 2 ? 12 : 0)) * time * Math.PI * 2) * 0.09 * envelope;
    const drumTime = time % beat;
    const kick = Math.sin(2 * Math.PI * (70 * drumTime + 2 * (1 - Math.exp(-drumTime * 40)))) * Math.exp(-drumTime * 24) * 0.13;
    const hat = (rng.next() * 2 - 1) * Math.exp(-local * 100) * (intense ? 0.055 : 0.02);
    const harmony = intense && note >= 0 ? Math.sin(hz(note - 12) * time * Math.PI * 2) * envelope * 0.045 : 0;
    // Bordes suaves: no hay salto de amplitud al repetir el buffer.
    const edge = Math.min(1, i / (sampleRate * 0.006), (samples.length - 1 - i) / (sampleRate * 0.006));
    samples[i] = (lead + low + kick + hat + harmony) * edge;
  }
  return samples;
}
