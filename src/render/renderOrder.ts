// Orden de dibujo (renderOrder de Three). Entre los objetos opacos importa por la
// silueta del jugador: se dibuja después de todo lo que puede taparla (mundo y
// enemigos) y justo antes que el propio jugador. Lo que no debe revelarla (hierba,
// fauna, partículas, gemas, proyectiles) se dibuja después. Los transparentes
// (sombras, aura, números) van siempre después de los opacos, en su propio orden.
export const RENDER_ORDER = {
  sky: -1,
  /** Mundo y enemigos: pueden tapar a Doña Remedios y hacen que se vea su silueta. */
  default: 0,
  playerSilhouette: 4,
  player: 5,
  /** Cosas pequeñas o de paso que no deben revelar la silueta. */
  afterPlayer: 6,
  // Transparentes
  blobShadow: 1,
  aura: 2,
  damageNumbers: 10,
} as const;
