import { afterEach, describe, expect, it } from 'vitest';
import { CHARACTERS } from '../data/characters';
import { TOMES } from '../data/tomes';
import { setLanguage } from '../i18n';
import { computePlayerStats, tomeBonuses } from '../systems/stats';
import { describeCard, statLines, type OwnedLevels } from './cards';

const owned = (weapon = 1, tome = 0): OwnedLevels => ({ weapon: () => weapon, tome: () => tome });

describe('textos de las cartas', () => {
  afterEach(() => setLanguage('es'));

  it('una mejora de arma enseña rareza, niveles y cada estadística con su nombre', () => {
    setLanguage('es');
    const card = describeCard(
      {
        kind: 'weaponUpgrade',
        key: 'weapon:jersey',
        weapon: 'jersey',
        rarity: 'rare',
        changes: [
          { stat: 'damage', amount: 0.4125 },
          { stat: 'pierce', amount: 1 },
        ],
      },
      owned(2),
    );
    expect(card.tone).toBe('rare');
    expect(card.tag).toBe('Rara');
    expect(card.title).toBe('Jersey Estático');
    expect(card.level).toBe('Nv 2 → 3');
    // La perforación del jersey se llama "saltos"; los decimales, con coma.
    expect(card.lines).toEqual(['Daño +41,3 %', 'Saltos +1']);
    expect(card.banishable).toBe(true);
  });

  it('en inglés, mismo contenido con su formato', () => {
    setLanguage('en');
    const card = describeCard(
      { kind: 'weaponUpgrade', key: 'weapon:chancla', weapon: 'chancla', rarity: 'common', changes: [{ stat: 'cooldown', amount: 0.12 }] },
      owned(1),
    );
    expect(card.level).toBe('Lv 1 → 2');
    expect(card.lines).toEqual(['Attack speed +12%']);
  });

  it('armas y tomos nuevos llevan su descripción; el relleno no se puede descartar', () => {
    setLanguage('es');
    const weapon = describeCard({ kind: 'newWeapon', key: 'weapon:fregona', weapon: 'fregona' }, owned());
    expect(weapon.tone).toBe('new');
    expect(weapon.tag).toBe('Arma nueva');
    expect(weapon.description).toContain('fregado');
    const tome = describeCard({ kind: 'tome', key: 'tome:vitality', tome: 'vitality', rarity: 'common', amounts: [20, 0.4] }, owned(1, 0));
    expect(tome.level).toBe('Tomo nuevo');
    expect(tome.lines).toEqual(['Vida máxima +20', 'Vida por segundo +0,4']);
    expect(tome.description).not.toBe('');
    const upgrade = describeCard({ kind: 'tome', key: 'tome:magnet', tome: 'magnet', rarity: 'epic', amounts: [0.525, 0.168] }, owned(1, 2));
    expect(upgrade.level).toBe('Nv 2 → 3');
    expect(upgrade.lines).toEqual(['Radio de recogida +52,5 %', 'Experiencia +16,8 %']);
    const heal = describeCard({ kind: 'heal', key: null, amount: 0.3 }, owned());
    expect(heal.banishable).toBe(false);
    expect(heal.description).toContain('30 %');
  });
});

describe('estadísticas de la pausa', () => {
  afterEach(() => setLanguage('es'));

  it('muestran la vida, los porcentajes de mejora y el radio en metros', () => {
    setLanguage('es');
    const stats = computePlayerStats(
      CHARACTERS.remedios,
      tomeBonuses([
        { def: TOMES.damage, level: 2, bonus: [0.24] },
        { def: TOMES.magnet, level: 1, bonus: [0.25, 0.08] },
      ]),
    );
    const lines = new Map(statLines(stats, 87.2).map((l) => [l.label, l.value]));
    expect(lines.get('Vida')).toBe('88 / 100');
    expect(lines.get('Daño')).toBe('+24 %');
    expect(lines.get('Velocidad de ataque')).toBe('+0 %');
    expect(lines.get('Radio de recogida')).toBe('4 m');
    expect(lines.get('Experiencia')).toBe('+8 %');
  });
});
