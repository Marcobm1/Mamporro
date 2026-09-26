// Enemigos. Cada entrada define sus estadísticas base (a los 0 minutos); la
// vida se multiplica con el tiempo según la curva de data/waves.ts.
import type { TranslationKey } from '../i18n';

export type EnemyId = 'pelusa' | 'cucaracha' | 'taper' | 'paloma' | 'rata' | 'pelusaMadre';

/**
 * Cómo se comporta:
 * - 'chase': va a por el jugador;
 * - 'ranged': mantiene la distancia y dispara con un aviso previo;
 * - 'charger': persigue y, de vez en cuando, embiste en línea recta tras avisar;
 * - 'boss': lo mueve el controlador del jefe (systems/BossController.ts).
 */
export type EnemyBehavior = 'chase' | 'ranged' | 'charger' | 'boss';

export interface RangedParams {
  /** Distancia a la que intenta quedarse (m). */
  preferred: number;
  /** Distancia máxima a la que dispara (m). */
  range: number;
  /** Segundos entre disparos. */
  cooldown: number;
  /** Aviso antes de disparar (se para y se hincha). */
  windup: number;
  projectileSpeed: number;
  projectileDamage: number;
  projectileRadius: number;
}

export interface ChargeParams {
  /** Segundos entre embestidas. */
  cooldown: number;
  /** Distancia máxima a la que decide embestir (m). */
  range: number;
  /** Aviso antes de embestir (se para, brilla y marca la dirección). */
  windup: number;
  /** Duración y velocidad de la embestida. */
  dashTime: number;
  dashSpeed: number;
  /** Multiplicador del daño por contacto durante la embestida. */
  damageMultiplier: number;
  /** Segundos quieto tras embestir (el momento de castigarle). */
  recover: number;
}

export interface EnemyDef {
  id: EnemyId;
  nameKey: TranslationKey;
  behavior: EnemyBehavior;
  hp: number;
  /** Velocidad máxima (m/s). La del jugador es 9,5. */
  speed: number;
  /** Aceleración hacia la velocidad deseada (1/s): valores altos = giros bruscos. */
  agility: number;
  /** Daño por contacto. */
  damage: number;
  /** Radio de colisión (m). */
  radius: number;
  /** Altura aproximada (para números de daño y partículas). */
  height: number;
  /** Experiencia que suelta al morir. */
  xp: number;
  /** Resistencia al empuje: el retroceso se divide entre la masa (y frena más al atravesarlo). */
  mass: number;
  /** Oro que suelta: probabilidad y cantidad. */
  gold: { chance: number; min: number; max: number };
  /** Élite o jefe: se avisa al aparecer y no se recicla aunque se quede lejos. */
  special?: 'elite' | 'boss';
  ranged?: RangedParams;
  charge?: ChargeParams;
  /** Colores de las partículas al morir. */
  debrisColors: readonly number[];
}

export const ENEMIES: Readonly<Record<EnemyId, EnemyDef>> = {
  pelusa: {
    id: 'pelusa',
    nameKey: 'enemy.pelusa',
    behavior: 'chase',
    hp: 14,
    speed: 4.2,
    agility: 5,
    damage: 8,
    radius: 0.5,
    height: 0.95,
    xp: 1,
    mass: 1,
    gold: { chance: 0.08, min: 1, max: 1 },
    debrisColors: [0xb8b4c4, 0x8f8a9e, 0xd8d4e0],
  },
  cucaracha: {
    id: 'cucaracha',
    nameKey: 'enemy.cucaracha',
    behavior: 'chase',
    hp: 7,
    speed: 7.2,
    agility: 9,
    damage: 5,
    radius: 0.42,
    height: 0.5,
    xp: 1,
    mass: 0.7,
    gold: { chance: 0.06, min: 1, max: 1 },
    debrisColors: [0x6b3a1e, 0x3a2012, 0xf0c040],
  },
  taper: {
    id: 'taper',
    nameKey: 'enemy.taper',
    behavior: 'chase',
    hp: 60,
    speed: 2.6,
    agility: 3,
    damage: 14,
    radius: 0.8,
    height: 1,
    xp: 4,
    mass: 4,
    gold: { chance: 0.35, min: 2, max: 4 },
    debrisColors: [0xcfe3ea, 0x7fb24a, 0x3f7fc0],
  },
  paloma: {
    id: 'paloma',
    nameKey: 'enemy.paloma',
    behavior: 'ranged',
    hp: 12,
    speed: 5,
    agility: 6,
    damage: 6,
    radius: 0.45,
    height: 0.8,
    xp: 2,
    mass: 0.8,
    gold: { chance: 0.2, min: 1, max: 2 },
    ranged: {
      preferred: 10,
      range: 16,
      cooldown: 2.6,
      windup: 0.5,
      projectileSpeed: 11,
      projectileDamage: 7,
      projectileRadius: 0.3,
    },
    debrisColors: [0x8a8f9c, 0x5a5f6c, 0x6fae8a],
  },
  rata: {
    id: 'rata',
    nameKey: 'enemy.rata',
    behavior: 'charger',
    hp: 420,
    speed: 5.2,
    agility: 5,
    damage: 20,
    radius: 0.9,
    height: 1.5,
    xp: 40,
    mass: 6,
    gold: { chance: 1, min: 20, max: 30 },
    special: 'elite',
    charge: { cooldown: 3.5, range: 14, windup: 0.8, dashTime: 0.55, dashSpeed: 19, damageMultiplier: 1.5, recover: 0.7 },
    debrisColors: [0x7a6a62, 0xe07f92, 0xff4a4a],
  },
  pelusaMadre: {
    id: 'pelusaMadre',
    nameKey: 'enemy.pelusaMadre',
    behavior: 'boss',
    hp: 3000,
    speed: 3.4,
    agility: 3,
    damage: 25,
    radius: 2.4,
    height: 4.4,
    xp: 0,
    mass: 50,
    gold: { chance: 0, min: 0, max: 0 },
    special: 'boss',
    debrisColors: [0xb8b4c4, 0xd8d4e0, 0xf0c040],
  },
};

/** Lista en orden fijo: el índice de cada enemigo es su "tipo" en los arrays del sistema. */
export const ENEMY_LIST: readonly EnemyDef[] = [
  ENEMIES.pelusa,
  ENEMIES.cucaracha,
  ENEMIES.taper,
  ENEMIES.paloma,
  ENEMIES.rata,
  ENEMIES.pelusaMadre,
];

export function enemyTypeIndex(id: EnemyId): number {
  return ENEMY_LIST.findIndex((e) => e.id === id);
}

/** Los tres ataques avisados de la Pelusa Madre (systems/BossController.ts). */
export const BOSS_CONFIG = {
  enemy: 'pelusaMadre' as EnemyId,
  /**
   * Vida: base × (1 + hpGrowth·min + hpCurve·min²), según el minuto de dificultad
   * en que se la invoca. Crece más deprisa que la de los enemigos porque el daño
   * del jugador también se dispara con la partida.
   */
  hpGrowth: 0.2,
  hpCurve: 0.06,
  /** Segundos persiguiendo entre ataque y ataque (al azar entre los dos). */
  idle: [1.6, 2.6] as const,
  /** Segundos quieta tras cada ataque (el momento de castigarla). */
  recover: 0.9,
  /** Por debajo de esta fracción de vida se enfada: todo va más rápido y estornuda más. */
  enrageAt: 0.5,
  enrageSpeed: 1.5,
  /** Rodillo: avisa con una franja en el suelo y rueda en línea recta. */
  roll: { windup: 0.9, speed: 18, length: 22, damage: 28 },
  /** Culetazo: avisa con un círculo a su alrededor y cae con todo su peso. */
  slam: { windup: 1.3, radius: 5.5, damage: 32 },
  /** Estornudo: se hincha y suelta bolas de polvo en todas direcciones y pelusas hijas. */
  sneeze: { windup: 0.8, projectiles: 16, enragedProjectiles: 24, speed: 8, damage: 10, radius: 0.45, minions: 5 },
} as const;

export type BossAttack = 'roll' | 'slam' | 'sneeze';
export const BOSS_ATTACKS: readonly BossAttack[] = ['roll', 'slam', 'sneeze'];
