// Enemigos. Cada entrada define sus estadísticas base (a los 0 minutos); la
// vida se multiplica con el tiempo según la curva de data/waves.ts.
import type { TranslationKey } from '../i18n';

export type EnemyId = 'pelusa' | 'cucaracha';

export interface EnemyDef {
  id: EnemyId;
  nameKey: TranslationKey;
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
  /** Resistencia al empuje: el retroceso se divide entre la masa. */
  mass: number;
  /** Colores de las partículas al morir. */
  debrisColors: readonly number[];
}

export const ENEMIES: Readonly<Record<EnemyId, EnemyDef>> = {
  pelusa: {
    id: 'pelusa',
    nameKey: 'enemy.pelusa',
    hp: 14,
    speed: 4.2,
    agility: 5,
    damage: 8,
    radius: 0.5,
    height: 0.95,
    xp: 1,
    mass: 1,
    debrisColors: [0xb8b4c4, 0x8f8a9e, 0xd8d4e0],
  },
  cucaracha: {
    id: 'cucaracha',
    nameKey: 'enemy.cucaracha',
    hp: 7,
    speed: 7.2,
    agility: 9,
    damage: 5,
    radius: 0.42,
    height: 0.5,
    xp: 1,
    mass: 0.7,
    debrisColors: [0x6b3a1e, 0x3a2012, 0xf0c040],
  },
};

/** Lista en orden fijo: el índice de cada enemigo es su "tipo" en los arrays del sistema. */
export const ENEMY_LIST: readonly EnemyDef[] = [ENEMIES.pelusa, ENEMIES.cucaracha];

export function enemyTypeIndex(id: EnemyId): number {
  return ENEMY_LIST.findIndex((e) => e.id === id);
}
