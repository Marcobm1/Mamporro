// Timbres originales: frecuencia inicial/final, duración y presupuesto de voces.
export interface SoundDef {
  frequency: number;
  end: number;
  duration: number;
  gain: number;
  noise: number;
  priority: number;
  cooldown: number;
}
const sound = (frequency: number, end: number, duration: number, priority = 0, noise = 0, gain = 0.14, cooldown = 0.08): SoundDef =>
  ({ frequency, end, duration, gain, noise, priority, cooldown });
export const SOUNDS = {
  ui: sound(700, 950, 0.055),
  chancla: sound(320, 90, 0.14, 0, 0.35),
  naftalina: sound(150, 240, 0.2, 0, 0.2, 0.09),
  barra: sound(180, 65, 0.16, 0, 0.5),
  dentaduras: sound(550, 300, 0.09),
  jersey: sound(950, 180, 0.2, 0, 0.6),
  fregona: sound(220, 90, 0.18, 0, 0.7, 0.1),
  hit: sound(140, 65, 0.065, 0, 0.65, 0.08, 0.055),
  critical: sound(650, 160, 0.12, 0, 0.3),
  death: sound(170, 35, 0.18, 0, 0.65, 0.1),
  hurt: sound(240, 50, 0.35, 2, 0.25, 0.22),
  xp: sound(900, 1250, 0.07, 0, 0, 0.08, 0.12),
  gold: sound(1300, 1700, 0.1, 0, 0, 0.09, 0.14),
  reward: sound(450, 1400, 0.45, 1),
  level: sound(300, 1600, 0.6, 2),
  shield: sound(1100, 440, 0.35, 2, 0.1),
  boss: sound(180, 40, 0.8, 2, 0.4, 0.2),
  blast: sound(100, 25, 0.4, 1, 0.8),
  victory: sound(260, 1700, 1.1, 2, 0, 0.18),
  defeat: sound(500, 60, 0.9, 2, 0.1, 0.18),
} satisfies Record<string, SoundDef>;
export type SoundId = keyof typeof SOUNDS;
export const AUDIO_CONFIG = { sampleRate: 22050, voices: 16, reserved: 4, bpm: 132 } as const;
// Ocho compases, melodía sincopada y bajo saltarín; -1 es un silencio.
export const MELODY = [72, -1, 76, 79, 76, 72, 74, -1, 71, 74, 77, -1, 74, 71, 67, -1,
  69, -1, 72, 76, 79, 76, 72, 69, 67, 71, 74, -1, 79, 77, 74, -1] as const;
export const BASS = [48, 43, 47, 43, 45, 40, 43, 47] as const;
