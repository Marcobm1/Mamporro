// Modelos low-poly de los enemigos (miran hacia -Z). Una geometría por tipo,
// con colores de vértice, para dibujarlos instanciados.
import {
  BoxGeometry,
  ConeGeometry,
  CylinderGeometry,
  IcosahedronGeometry,
  Quaternion,
  Vector3,
  type BufferGeometry,
} from 'three';
import { Rng } from '../core/rng';
import type { EnemyId } from '../data/enemies';
import { colored, jitterVertices, mergeColored } from '../render/geometry';
import { PALETTE } from '../render/palette';

const UP = new Vector3(0, 1, 0);

/** Pelusa Rebelde: bola de polvo con pinchos de pelusa, ojos cabreados y patitas. */
function pelusa(): BufferGeometry {
  const rng = new Rng('model-pelusa');
  const parts: BufferGeometry[] = [];
  const body = jitterVertices(new IcosahedronGeometry(0.46, 1), rng, 0.06).translate(0, 0.48, 0);
  parts.push(colored(body, PALETTE.pelusa));
  // Mechones: conos pequeños saliendo en direcciones repartidas.
  const q = new Quaternion();
  for (let i = 0; i < 9; i++) {
    const dir = new Vector3(rng.range(-1, 1), rng.range(-0.2, 1), rng.range(-0.3, 1)).normalize();
    const tuft = new ConeGeometry(0.09, 0.26, 4).translate(0, 0.13, 0);
    tuft.applyQuaternion(q.setFromUnitVectors(UP, dir));
    tuft.translate(dir.x * 0.4, 0.48 + dir.y * 0.4, dir.z * 0.4);
    parts.push(colored(tuft, i % 2 ? PALETTE.pelusa : PALETTE.pelusaDark));
  }
  // Ojos con cejas en "V" (cara de pocos amigos)
  for (const side of [-1, 1]) {
    parts.push(colored(new BoxGeometry(0.15, 0.17, 0.05).translate(side * 0.14, 0.56, -0.42), PALETTE.eyeWhite));
    parts.push(colored(new BoxGeometry(0.07, 0.09, 0.03).translate(side * 0.11, 0.54, -0.452), PALETTE.pupil));
    parts.push(colored(new BoxGeometry(0.18, 0.04, 0.04).rotateZ(side * -0.45).translate(side * 0.14, 0.69, -0.43), PALETTE.brow));
    parts.push(colored(new BoxGeometry(0.1, 0.06, 0.16).translate(side * 0.16, 0.03, -0.12), PALETTE.brow));
  }
  parts.push(colored(new BoxGeometry(0.14, 0.03, 0.03).translate(0, 0.4, -0.44), PALETTE.pupil));
  return mergeColored(parts);
}

/** Cucaracha Turbo: cuerpo brillante con franja de carreras, antenas y seis patas. */
function cucaracha(): BufferGeometry {
  const parts: BufferGeometry[] = [];
  parts.push(colored(new IcosahedronGeometry(0.3, 1).scale(0.8, 0.45, 1.3).translate(0, 0.22, 0.05), PALETTE.roach));
  parts.push(colored(new BoxGeometry(0.03, 0.02, 0.62).translate(0, 0.355, 0.08), PALETTE.roachDark));
  parts.push(colored(new BoxGeometry(0.1, 0.02, 0.5).translate(0.09, 0.345, 0.06), PALETTE.roachStripe));
  parts.push(colored(new IcosahedronGeometry(0.14, 0).translate(0, 0.2, -0.36), PALETTE.roachDark));
  for (const side of [-1, 1]) {
    parts.push(colored(new BoxGeometry(0.05, 0.05, 0.03).translate(side * 0.06, 0.25, -0.48), PALETTE.eyeWhite));
    // Antenas hacia delante y arriba
    parts.push(colored(new BoxGeometry(0.018, 0.018, 0.55).translate(0, 0, -0.27).rotateX(0.5).rotateY(side * 0.35).translate(side * 0.05, 0.27, -0.44), PALETTE.roachDark));
    // Tres patas por lado
    for (const z of [-0.15, 0.06, 0.26]) {
      parts.push(colored(new BoxGeometry(0.3, 0.025, 0.025).rotateZ(side * -0.55).translate(side * 0.3, 0.1, z), PALETTE.roachDark));
    }
  }
  return mergeColored(parts);
}

/** Táper Caducado: fiambrera con la tapa medio abierta y lentejas con moho asomando. */
function taper(): BufferGeometry {
  const rng = new Rng('model-taper');
  const parts: BufferGeometry[] = [];
  parts.push(colored(new BoxGeometry(1.2, 0.62, 0.92).translate(0, 0.43, 0), PALETTE.taper));
  // Borde de la fiambrera (un poco más ancho) y la tapa abierta, con bisagra detrás.
  parts.push(colored(new BoxGeometry(1.26, 0.08, 0.98).translate(0, 0.74, 0), PALETTE.taperShade));
  parts.push(colored(new BoxGeometry(1.3, 0.09, 1.02).translate(0, 0, 0.51).rotateX(-0.55).translate(0, 0.8, -0.51 + 0.02), PALETTE.taperLid));
  parts.push(colored(new BoxGeometry(0.18, 0.08, 0.1).translate(0, 0, 1.0).rotateX(-0.55).translate(0, 0.8, -0.49), PALETTE.taperLid));
  // Lentejas con moho a punto de salirse.
  for (let i = 0; i < 7; i++) {
    const r = rng.range(0.14, 0.24);
    const color = i % 3 === 0 ? PALETTE.lentil : i % 2 ? PALETTE.mold : PALETTE.moldDark;
    parts.push(colored(jitterVertices(new IcosahedronGeometry(r, 0), rng, 0.03).translate(rng.range(-0.42, 0.42), 0.78, rng.range(-0.3, 0.3)), color));
  }
  // Cara en el frontal: ojos saltones, cejas y boca torcida.
  for (const side of [-1, 1]) {
    parts.push(colored(new BoxGeometry(0.22, 0.24, 0.05).translate(side * 0.25, 0.5, -0.475), PALETTE.eyeWhite));
    parts.push(colored(new BoxGeometry(0.1, 0.12, 0.03).translate(side * 0.22, 0.47, -0.505), PALETTE.pupil));
    parts.push(colored(new BoxGeometry(0.26, 0.05, 0.04).rotateZ(side * -0.35).translate(side * 0.25, 0.68, -0.48), PALETTE.brow));
    // Patitas
    for (const z of [-0.3, 0.3]) parts.push(colored(new BoxGeometry(0.12, 0.14, 0.12).translate(side * 0.45, 0.06, z), PALETTE.taperLid));
  }
  parts.push(colored(new BoxGeometry(0.4, 0.05, 0.04).rotateZ(0.12).translate(0, 0.28, -0.48), PALETTE.pupil));
  return mergeColored(parts);
}

/** Paloma Okupa: gris, con el cuello tornasolado, pico oscuro y patas rosas. Escupe pipas. */
function paloma(): BufferGeometry {
  const parts: BufferGeometry[] = [];
  parts.push(colored(new IcosahedronGeometry(0.3, 1).scale(0.85, 0.8, 1.2).translate(0, 0.42, 0.02), PALETTE.pigeon));
  parts.push(colored(new IcosahedronGeometry(0.19, 1).translate(0, 0.6, -0.18), PALETTE.pigeonNeck));
  parts.push(colored(new IcosahedronGeometry(0.17, 1).scale(1, 0.6, 1).translate(0, 0.53, -0.2), PALETTE.pigeonNeckPurple));
  parts.push(colored(new IcosahedronGeometry(0.15, 1).translate(0, 0.76, -0.27), PALETTE.pigeon));
  parts.push(colored(new ConeGeometry(0.05, 0.14, 4).rotateX(-Math.PI / 2).translate(0, 0.74, -0.45), PALETTE.beak));
  parts.push(colored(new BoxGeometry(0.07, 0.03, 0.05).translate(0, 0.78, -0.4), PALETTE.eyeWhite));
  for (const side of [-1, 1]) {
    parts.push(colored(new BoxGeometry(0.03, 0.07, 0.07).translate(side * 0.13, 0.79, -0.32), PALETTE.pigeonEye));
    parts.push(colored(new BoxGeometry(0.02, 0.035, 0.035).translate(side * 0.145, 0.79, -0.33), PALETTE.pupil));
    // Alas plegadas y patas
    parts.push(colored(new BoxGeometry(0.06, 0.24, 0.52).rotateZ(side * 0.25).translate(side * 0.27, 0.46, 0.06), PALETTE.pigeonDark));
    parts.push(colored(new BoxGeometry(0.05, 0.2, 0.05).translate(side * 0.09, 0.1, -0.02), PALETTE.pinkSkin));
    parts.push(colored(new BoxGeometry(0.1, 0.03, 0.14).translate(side * 0.09, 0.015, -0.06), PALETTE.pinkSkin));
  }
  parts.push(colored(new BoxGeometry(0.22, 0.04, 0.32).rotateX(0.3).translate(0, 0.42, 0.44), PALETTE.pigeonDark));
  return mergeColored(parts);
}

/** Rata de Gimnasio (élite): musculada, con muñequera, mancuerna y cola rosa. Embiste. */
function rata(): BufferGeometry {
  const parts: BufferGeometry[] = [];
  parts.push(colored(new IcosahedronGeometry(0.55, 1).scale(0.95, 0.85, 1.05).translate(0, 0.78, 0.05), PALETTE.rat));
  parts.push(colored(new IcosahedronGeometry(0.38, 1).scale(1, 1, 0.6).translate(0, 0.72, -0.3), PALETTE.ratBelly));
  parts.push(colored(new IcosahedronGeometry(0.32, 1).scale(0.95, 0.85, 1.15).translate(0, 1.2, -0.42), PALETTE.rat));
  parts.push(colored(new ConeGeometry(0.16, 0.34, 6).rotateX(-Math.PI / 2).translate(0, 1.13, -0.8), PALETTE.rat));
  parts.push(colored(new IcosahedronGeometry(0.06, 0).translate(0, 1.13, -0.98), PALETTE.pinkSkin));
  parts.push(colored(new CylinderGeometry(0.31, 0.31, 0.08, 10).translate(0, 1.36, -0.42), PALETTE.sweatband));
  for (const side of [-1, 1]) {
    parts.push(colored(new IcosahedronGeometry(0.13, 0).scale(1, 1, 0.4).translate(side * 0.23, 1.46, -0.36), PALETTE.pinkSkin));
    parts.push(colored(new BoxGeometry(0.07, 0.07, 0.04).translate(side * 0.12, 1.26, -0.73), PALETTE.ratEye));
    parts.push(colored(new BoxGeometry(0.16, 0.04, 0.04).rotateZ(side * -0.4).translate(side * 0.13, 1.35, -0.7), PALETTE.brow));
    // Brazos de gimnasio: bíceps y antebrazo
    parts.push(colored(new IcosahedronGeometry(0.22, 1).translate(side * 0.58, 0.98, -0.1), PALETTE.rat));
    parts.push(colored(new IcosahedronGeometry(0.17, 1).translate(side * 0.64, 0.64, -0.3), PALETTE.ratDark));
    parts.push(colored(new CylinderGeometry(0.19, 0.19, 0.07, 8).translate(side * 0.64, 0.8, -0.3), PALETTE.sweatband));
    // Piernas
    parts.push(colored(new IcosahedronGeometry(0.2, 0).scale(1, 0.8, 1.2).translate(side * 0.3, 0.2, 0.12), PALETTE.ratDark));
  }
  // Mancuerna en la mano derecha
  parts.push(colored(new BoxGeometry(0.06, 0.06, 0.5).translate(0.66, 0.5, -0.46), PALETTE.dumbbellBar));
  for (const z of [-0.7, -0.22]) parts.push(colored(new CylinderGeometry(0.14, 0.14, 0.1, 8).rotateX(Math.PI / 2).translate(0.66, 0.5, z), PALETTE.dumbbell));
  // Cola
  parts.push(colored(new BoxGeometry(0.06, 0.06, 0.9).translate(0, 0, 0.45).rotateX(0.35).translate(0, 0.4, 0.5), PALETTE.pinkSkin));
  return mergeColored(parts);
}

/** La Pelusa Madre (jefe): una pelusa gigantesca con rulos, ojos rojos y boca de pocos amigos. */
function pelusaMadre(): BufferGeometry {
  const rng = new Rng('model-pelusa-madre');
  const parts: BufferGeometry[] = [];
  const body = jitterVertices(new IcosahedronGeometry(2.05, 2), rng, 0.22).translate(0, 2.15, 0);
  parts.push(colored(body, PALETTE.pelusa));
  const q = new Quaternion();
  for (let i = 0; i < 30; i++) {
    const dir = new Vector3(rng.range(-1, 1), rng.range(-0.3, 1), rng.range(-0.4, 1)).normalize();
    const tuft = new ConeGeometry(0.34, 1.05, 4).translate(0, 0.52, 0);
    tuft.applyQuaternion(q.setFromUnitVectors(UP, dir));
    tuft.translate(dir.x * 1.85, 2.15 + dir.y * 1.85, dir.z * 1.85);
    parts.push(colored(tuft, i % 2 ? PALETTE.pelusa : PALETTE.pelusaDark));
  }
  for (const side of [-1, 1]) {
    parts.push(colored(new BoxGeometry(0.62, 0.7, 0.2).translate(side * 0.62, 2.6, -1.93), PALETTE.eyeWhite));
    parts.push(colored(new BoxGeometry(0.3, 0.34, 0.1).translate(side * 0.52, 2.52, -2.05), PALETTE.bossPupil));
    parts.push(colored(new BoxGeometry(0.12, 0.14, 0.06).translate(side * 0.5, 2.55, -2.11), PALETTE.pupil));
    parts.push(colored(new BoxGeometry(0.8, 0.16, 0.16).rotateZ(side * -0.5).translate(side * 0.62, 3.1, -1.92), PALETTE.brow));
    parts.push(colored(new BoxGeometry(0.5, 0.3, 0.7).translate(side * 0.8, 0.14, -0.6), PALETTE.brow));
  }
  // Boca abierta con dos dientes
  parts.push(colored(new BoxGeometry(1.0, 0.38, 0.12).translate(0, 1.72, -2.0), PALETTE.mouth));
  for (const side of [-1, 1]) parts.push(colored(new BoxGeometry(0.2, 0.2, 0.06).translate(side * 0.26, 1.82, -2.07), PALETTE.eyeWhite));
  // Rulos rosas en lo alto
  const curlers: Array<[number, number, number, number]> = [
    [-0.75, 4.05, 0.1, 0.3],
    [0.05, 4.3, -0.25, -0.2],
    [0.8, 4.02, 0.15, 0.25],
  ];
  for (const [x, y, z, rot] of curlers) {
    parts.push(colored(new CylinderGeometry(0.26, 0.26, 0.85, 8).rotateZ(Math.PI / 2).rotateY(rot).translate(x, y, z), PALETTE.curler));
  }
  return mergeColored(parts);
}

export const ENEMY_MODELS: Readonly<Record<EnemyId, () => BufferGeometry>> = {
  pelusa,
  cucaracha,
  taper,
  paloma,
  rata,
  pelusaMadre,
};
