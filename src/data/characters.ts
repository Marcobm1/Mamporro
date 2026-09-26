// Personajes jugables. En el hito 5 llegan el segundo personaje, las pasivas y
// la selección; de momento solo existe Doña Remedios.
import type { TranslationKey } from '../i18n';
import type { WeaponId } from './weapons';

export type CharacterId = 'remedios';

export interface CharacterDef {
  id: CharacterId;
  nameKey: TranslationKey;
  startingWeapon: WeaponId;
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
    maxHp: 100,
    armor: 0,
    pickupRadius: 3.2,
  },
};
