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
  ivy: 0x2f6b2f,
  grassTuft: 0x4c9a34,
  grassTuftTip: 0xa6d65a,
  flowerRed: 0xe0404a,
  flowerYellow: 0xf0d040,
  flowerWhite: 0xf4f0e8,
  flowerPurple: 0xa060d0,
  mushroomRed: 0xd8342c,
  mushroomBrown: 0x9a6a44,
  mushroomStem: 0xf0e6d0,

  // Construcciones
  stoneLight: 0xd2cab8,
  stone: 0xb4ab9a,
  stoneDark: 0x8c8477,
  marble: 0xe8e4dc,
  cobble: 0xa39b8b,
  cobbleDark: 0x857d70,
  wood: 0x9a6a3a,
  woodDark: 0x6a4424,
  woodLight: 0xc49a5e,
  roof: 0xb5543a,
  straw: 0xe0c060,
  strawDark: 0xc8a040,
  plaid: 0xc0392b,
  burlap: 0xc8a878,
  hat: 0x3a2e2a,
  water: 0x2a4a6a,

  // Fauna
  bird: 0x2e2a3a,
  butterflyA: 0xf6e27a,
  butterflyB: 0xf4f4f4,
  butterflyC: 0x7ab8f6,

  // Doña Remedios
  skin: 0xf2c6a0,
  tights: 0xd8b48a,
  hairGrey: 0xd9d9e6,
  hairLavender: 0xc9c1e4,
  dress: 0x9b4f96,
  dressDark: 0x6f3470,
  apron: 0xf1e9d6,
  apronPocket: 0x8fb8e8,
  cardigan: 0xe8d6a8,
  shawl: 0x5e3d7a,
  button: 0x6b4226,
  slipper: 0x3f7fd6,
  pompom: 0xff9ac8,
  chancla: 0xe8b020,
  chanclaStrap: 0xc4581c,
  glasses: 0x2a2233,
  lens: 0xd6ecff,
  cheek: 0xe8909a,
  gold: 0xf0c040,
  bag: 0x8a2e3b,
} as const;

export type PaletteColor = keyof typeof PALETTE;
