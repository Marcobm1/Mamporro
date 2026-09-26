// Estado de los interactuables durante una partida (lógica pura): cuáles se han
// descubierto, cuáles se han usado, la carga de los santuarios y el desafío del
// tótem en marcha. La partida decide qué pasa al usarlos (gastar oro, invocar...).
import { chestCost, DISCOVERY_CONFIG, PORTAL_CONFIG, SHRINE_CONFIG, TOTEM_CONFIG, CHEST_CONFIG, type InteractableKind } from '../data/run';
import type { InteractableSpot } from '../world/interactables';

export interface InteractableState {
  readonly spot: InteractableSpot;
  /** Visto de cerca: aparece en el minimapa. */
  discovered: boolean;
  /** Cofre abierto, santuario completado, tótem activado o portal abierto. */
  used: boolean;
  /** Carga del santuario (0..1). */
  charge: number;
}

/** Lo que el jugador tiene delante y puede usar con la tecla de interactuar. */
export interface InteractPrompt {
  index: number;
  kind: InteractableKind;
  /** Precio (solo cofres). */
  cost: number;
}

export interface InteractableEvents {
  discovered(item: InteractableState): void;
  /** Santuario cargado del todo: toca elegir bendición. */
  shrineCharged(item: InteractableState): void;
}

const REACH: Readonly<Record<InteractableKind, number>> = {
  chest: CHEST_CONFIG.reach,
  totem: TOTEM_CONFIG.reach,
  portal: PORTAL_CONFIG.reach,
  shrine: 0,
};

export class Interactables {
  readonly list: InteractableState[];
  chestsOpened = 0;
  /** Segundos que quedan del desafío del tótem (0 = ninguno en marcha). */
  challenge = 0;
  /** Índice del santuario que se está cargando ahora mismo (-1 = ninguno). */
  charging = -1;

  constructor(spots: readonly InteractableSpot[]) {
    this.list = spots.map((spot) => ({ spot, discovered: false, used: false, charge: 0 }));
  }

  get nextChestCost(): number {
    return chestCost(this.chestsOpened);
  }

  /**
   * Un tick: descubre lo cercano, carga (o descarga) los santuarios y avanza el
   * desafío. Devuelve true si el desafío del tótem ha terminado en este tick.
   */
  update(dt: number, x: number, z: number, events: InteractableEvents): boolean {
    this.charging = -1;
    this.list.forEach((item, index) => {
      const d = Math.hypot(item.spot.x - x, item.spot.z - z);
      if (!item.discovered && d <= (item.spot.kind === 'portal' ? DISCOVERY_CONFIG.portalRadius : DISCOVERY_CONFIG.radius)) {
        item.discovered = true;
        events.discovered(item);
      }
      if (item.spot.kind !== 'shrine' || item.used) return;
      if (d <= SHRINE_CONFIG.radius) {
        this.charging = index;
        item.charge = Math.min(1, item.charge + dt / SHRINE_CONFIG.chargeTime);
        if (item.charge >= 1) {
          item.used = true;
          events.shrineCharged(item);
        }
      } else {
        item.charge = Math.max(0, item.charge - (dt / SHRINE_CONFIG.chargeTime) * SHRINE_CONFIG.decayRate);
      }
    });
    if (this.challenge > 0) {
      this.challenge = Math.max(0, this.challenge - dt);
      return this.challenge === 0;
    }
    return false;
  }

  /** El interactuable usable más cercano al alcance del jugador (o null). */
  prompt(x: number, z: number): InteractPrompt | null {
    let best: InteractPrompt | null = null;
    let bestD = Number.POSITIVE_INFINITY;
    this.list.forEach((item, index) => {
      if (item.used || item.spot.kind === 'shrine') return;
      if (item.spot.kind === 'totem' && this.challenge > 0) return;
      const d = Math.hypot(item.spot.x - x, item.spot.z - z);
      if (d > REACH[item.spot.kind] || d >= bestD) return;
      bestD = d;
      best = { index, kind: item.spot.kind, cost: item.spot.kind === 'chest' ? this.nextChestCost : 0 };
    });
    return best;
  }

  /** Marca como descubiertos todos los de un tipo (o todos). Devuelve cuántos eran nuevos. */
  reveal(kind?: InteractableKind): number {
    let n = 0;
    for (const item of this.list) {
      if (item.discovered || (kind && item.spot.kind !== kind)) continue;
      item.discovered = true;
      n++;
    }
    return n;
  }

  find(kind: InteractableKind): InteractableState | undefined {
    return this.list.find((item) => item.spot.kind === kind);
  }
}
