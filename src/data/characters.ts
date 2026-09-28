// Personajes y pasivas únicas; los parámetros de balance viven aquí.
import type { TranslationKey } from '../i18n';
import type { WeaponId } from './weapons';

export type CharacterId = 'remedios' | 'baguette';

export interface CharacterDef {
  id: CharacterId;
  nameKey: TranslationKey;
  startingWeapon: WeaponId;
  passiveKey: TranslationKey;
  passive: { kind: 'slow'; radius: number; amount: number } | { kind: 'shield'; recharge: number };
  maxHp: number;
  armor: number;
  /** Radio de recogida de gemas de experiencia (m). */
  pickupRadius: number;
}

export const CHARACTERS: Readonly<Record<CharacterId, CharacterDef>> = {
  remedios: {
    id: 'remedios',
    nameKey: 'character.remedios',
    startingWeapon: 'chancla',
    passiveKey: 'character.remedios.passive',
    passive: { kind: 'slow', radius: 3, amount: 0.2 },
    maxHp: 100,
    armor: 0,
    pickupRadius: 3.2,
  },
  baguette: {
    id: 'baguette', nameKey: 'character.baguette', startingWeapon: 'barra',
    passiveKey: 'character.baguette.passive', passive: { kind: 'shield', recharge: 8 },
    maxHp: 100, armor: 0, pickupRadius: 3.2,
  },
};
