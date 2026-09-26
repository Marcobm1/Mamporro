// Paleta limitada del juego. Todo el color del mundo sale de aquí para mantener
// una estética coherente (cielo, terreno, vegetación y personajes).

export const PALETTE = {
  // Cielo y niebla (el horizonte y la niebla comparten color para fundirse).
  skyZenith: 0x5b8fd6,
  skyHorizon: 0xcfd9e8,
  sun: 0xfff1b8,
  sunLight: 0xfff4de,
  hemiSky: 0xc9dcff,
  hemiGround: 0x6b5a3e,

  // Terreno
  grassLight: 0x8cc84b,
  grass: 0x5fa83a,
  grassDark: 0x3f7f2e,
  moss: 0x566f2c,
  dirt: 0xa37a45,
  sand: 0xd9c084,
  rockLight: 0x9a93a8,
  rock: 0x77708a,
  rockDark: 0x575268,
  snow: 0xeef2f7,

  // Vegetación
  bark: 0x6e4a2e,
  leaf: 0x2f7d3b,
  leafLight: 0x4fa84a,
  leafYellow: 0xa8c040,
  leafOrange: 0xd08a3a,
  pine: 0x2a5e3f,
  bush: 0x3b8a3e,

  // Doña Remedios
  skin: 0xf2c6a0,
  tights: 0xd8b48a,
  hairGrey: 0xd9d9e6,
  dress: 0x9b4f96,
  cardigan: 0xe8d6a8,
  slipper: 0x3f7fd6,
  chancla: 0xe8b020,
  glasses: 0x2a2233,
} as const;

export type PaletteColor = keyof typeof PALETTE;
