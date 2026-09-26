// Experiencia y niveles del jugador (lógica pura).

/** Experiencia necesaria para pasar del nivel `level` al siguiente. */
export function xpToNextLevel(level: number): number {
  return Math.round(5 + 4 * level + 0.9 * Math.pow(level, 1.6));
}

export interface LevelState {
  level: number;
  xp: number;
}

/** Suma experiencia (puede subir varios niveles de golpe). Devuelve los niveles ganados. */
export function addExperience(state: LevelState, amount: number): number {
  state.xp += amount;
  let gained = 0;
  let needed = xpToNextLevel(state.level);
  while (state.xp >= needed) {
    state.xp -= needed;
    state.level++;
    gained++;
    needed = xpToNextLevel(state.level);
  }
  return gained;
}
