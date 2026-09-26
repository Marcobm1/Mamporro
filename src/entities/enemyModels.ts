// Modelos low-poly de los enemigos (miran hacia -Z). Una geometría por tipo,
// con colores de vértice, para dibujarlos instanciados.
import {
  BoxGeometry,
  ConeGeometry,
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

export const ENEMY_MODELS: Readonly<Record<EnemyId, () => BufferGeometry>> = {
  pelusa,
  cucaracha,
};
