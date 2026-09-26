// Aspecto de Doña Remedios: modelo low-poly hecho con primitivas, animación
// procedural (correr, saltar, deslizarse) y sombra de mancha bajo los pies.
import {
  BoxGeometry,
  CylinderGeometry,
  Group,
  IcosahedronGeometry,
  Mesh,
  MeshBasicMaterial,
  MeshLambertMaterial,
  PlaneGeometry,
  Quaternion,
  SphereGeometry,
  Vector3,
  type Object3D,
  type Texture,
} from 'three';
import { clamp, damp, lerp, type Vec3Like } from '../core/math';
import { PLAYER_BASE_STATS } from '../data/config';
import { colored, mergeColored } from '../render/geometry';
import { PALETTE } from '../render/palette';
import { applyRetro } from '../render/retroMaterial';
import type { PlayerBody } from './playerPhysics';

const HIP_HEIGHT = 0.46;
const UP = new Vector3(0, 1, 0);

/** Eventos de física acumulados desde el último frame (puede haber varios ticks por frame). */
export interface PlayerFrameEvents {
  landed: boolean;
  landingSpeed: number;
}

export interface PlayerRenderState {
  x: number;
  y: number;
  z: number;
  facing: number;
  /** Altura del suelo bajo el jugador (para la sombra). */
  groundY: number;
  groundNormal: Vec3Like;
}

function buildLeg(side: number): Mesh['geometry'] {
  const x = side * 0.1;
  return mergeColored([
    colored(new BoxGeometry(0.11, 0.4, 0.11).translate(x, -0.2, 0), PALETTE.tights),
    // Zapatilla de estar por casa con pompón
    colored(new BoxGeometry(0.15, 0.08, 0.27).translate(x, -0.42, -0.045), PALETTE.slipper),
    colored(new IcosahedronGeometry(0.045, 0).translate(x, -0.36, -0.14), PALETTE.pompom),
  ]);
}

function buildArm(side: number, holds: 'bag' | 'chancla'): Mesh['geometry'] {
  const parts = [
    colored(new BoxGeometry(0.09, 0.36, 0.09).translate(0, -0.18, 0), PALETTE.cardigan),
    colored(new BoxGeometry(0.08, 0.08, 0.08).translate(0, -0.4, 0), PALETTE.skin),
  ];
  if (holds === 'chancla') {
    // La chancla: suela amarilla y tira naranja.
    parts.push(
      colored(new BoxGeometry(0.11, 0.03, 0.27).translate(0, -0.45, -0.08), PALETTE.chancla),
      colored(new BoxGeometry(0.1, 0.035, 0.06).translate(0, -0.425, -0.14), PALETTE.chanclaStrap),
    );
  } else {
    // El bolso, colgando de la mano (se balancea con el brazo).
    parts.push(
      colored(new BoxGeometry(0.12, 0.018, 0.018).translate(0, -0.44, 0), PALETTE.bag),
      colored(new BoxGeometry(0.17, 0.13, 0.08).translate(0, -0.52, 0), PALETTE.bag),
      colored(new BoxGeometry(0.04, 0.03, 0.02).translate(0, -0.48, -0.045), PALETTE.gold),
    );
  }
  const g = mergeColored(parts);
  g.translate(side * 0.25, 0, 0);
  return g;
}

/** Rizos de la permanente alrededor de la cabeza (la cara, hacia -Z, queda libre). */
function buildCurls(): Mesh['geometry'][] {
  const curls: Mesh['geometry'][] = [];
  const around = [65, 110, 155, 205, 250, 295];
  around.forEach((deg, i) => {
    const a = (deg * Math.PI) / 180;
    const y = i % 2 === 0 ? 0.9 : 0.99;
    const color = i % 2 === 0 ? PALETTE.hairGrey : PALETTE.hairLavender;
    curls.push(colored(new IcosahedronGeometry(0.065, 0).translate(Math.sin(a) * 0.165, y, -Math.cos(a) * 0.165), color));
  });
  const top: Array<[number, number, number]> = [
    [0.07, 1.07, 0.03],
    [-0.07, 1.07, 0.03],
    [0, 1.09, -0.05],
    [0, 1.06, 0.1],
  ];
  top.forEach(([x, y, z], i) => {
    curls.push(colored(new IcosahedronGeometry(0.07, 0).translate(x, y, z), i % 2 ? PALETTE.hairLavender : PALETTE.hairGrey));
  });
  return curls;
}

function buildUpperBody(): Mesh['geometry'] {
  // El delantal sigue la inclinación del vestido (un cono de 0.36 a 0.2 de radio).
  const apronTilt = Math.atan2(0.16, 0.56);
  return mergeColored([
    // Vestido con franja oscura en el bajo
    colored(new CylinderGeometry(0.2, 0.36, 0.56, 7).translate(0, 0.2, 0), PALETTE.dress),
    colored(new CylinderGeometry(0.365, 0.37, 0.07, 7).translate(0, -0.045, 0), PALETTE.dressDark),
    // Delantal con bolsillo y cinta en la cintura
    colored(new BoxGeometry(0.32, 0.42, 0.03).rotateX(apronTilt).translate(0, 0.17, -0.305), PALETTE.apron),
    colored(new BoxGeometry(0.13, 0.08, 0.02).rotateX(apronTilt).translate(0.04, 0.1, -0.33), PALETTE.apronPocket),
    colored(new CylinderGeometry(0.212, 0.212, 0.04, 7).translate(0, 0.46, 0), PALETTE.apron),
    // Chaqueta de punto con botones y toquilla sobre los hombros
    colored(new CylinderGeometry(0.19, 0.22, 0.34, 7).translate(0, 0.62, 0), PALETTE.cardigan),
    colored(new BoxGeometry(0.03, 0.03, 0.02).translate(0, 0.53, -0.215), PALETTE.button),
    colored(new BoxGeometry(0.03, 0.03, 0.02).translate(0, 0.61, -0.205), PALETTE.button),
    colored(new CylinderGeometry(0.13, 0.27, 0.17, 7).translate(0, 0.72, 0.01), PALETTE.shawl),
    // Cabeza, pelo cardado y permanente
    colored(new IcosahedronGeometry(0.17, 0).translate(0, 0.92, 0), PALETTE.skin),
    colored(new SphereGeometry(0.18, 7, 4, 0, Math.PI * 2, 0, Math.PI * 0.5).translate(0, 0.96, 0.02), PALETTE.hairGrey),
    ...buildCurls(),
    // Cara (mira hacia -Z): gafas con cristales, nariz, coloretes y pendientes
    colored(new BoxGeometry(0.1, 0.075, 0.02).translate(-0.062, 0.94, -0.16), PALETTE.glasses),
    colored(new BoxGeometry(0.1, 0.075, 0.02).translate(0.062, 0.94, -0.16), PALETTE.glasses),
    colored(new BoxGeometry(0.066, 0.045, 0.012).translate(-0.062, 0.94, -0.171), PALETTE.lens),
    colored(new BoxGeometry(0.066, 0.045, 0.012).translate(0.062, 0.94, -0.171), PALETTE.lens),
    colored(new BoxGeometry(0.018, 0.018, 0.13).translate(-0.158, 0.945, -0.09), PALETTE.glasses),
    colored(new BoxGeometry(0.018, 0.018, 0.13).translate(0.158, 0.945, -0.09), PALETTE.glasses),
    colored(new BoxGeometry(0.05, 0.06, 0.06).translate(0, 0.885, -0.18), PALETTE.skin),
    colored(new BoxGeometry(0.05, 0.03, 0.02).translate(-0.095, 0.865, -0.148), PALETTE.cheek),
    colored(new BoxGeometry(0.05, 0.03, 0.02).translate(0.095, 0.865, -0.148), PALETTE.cheek),
    colored(new BoxGeometry(0.025, 0.035, 0.025).translate(-0.172, 0.86, -0.01), PALETTE.gold),
    colored(new BoxGeometry(0.025, 0.035, 0.025).translate(0.172, 0.86, -0.01), PALETTE.gold),
  ]);
}

export class PlayerView {
  readonly root = new Group();
  private readonly hips = new Group();
  private readonly upper = new Group();
  private readonly legL: Mesh;
  private readonly legR: Mesh;
  private readonly armL: Mesh;
  private readonly armR: Mesh;
  private readonly shadow: Mesh;
  private readonly shadowMaterial: MeshBasicMaterial;

  private phase = 0;
  private squash = 0;
  private slidePose = 0;
  private airPose = 0;
  private time = 0;
  private readonly quat = new Quaternion();
  private readonly normal = new Vector3();

  constructor(shadowTexture: Texture) {
    const material = applyRetro(new MeshLambertMaterial({ vertexColors: true, flatShading: true }));

    this.legL = new Mesh(buildLeg(-1), material);
    this.legR = new Mesh(buildLeg(1), material);
    this.armL = new Mesh(buildArm(-1, 'bag'), material);
    this.armR = new Mesh(buildArm(1, 'chancla'), material);
    const body = new Mesh(buildUpperBody(), material);

    // Los brazos giran desde el hombro.
    this.armL.position.set(0, 0.76, 0);
    this.armR.position.set(0, 0.76, 0);
    this.upper.add(body, this.armL, this.armR);
    this.hips.position.y = HIP_HEIGHT;
    this.hips.add(this.upper, this.legL, this.legR);
    this.root.add(this.hips);

    this.shadowMaterial = new MeshBasicMaterial({
      color: 0x000000,
      map: shadowTexture,
      transparent: true,
      opacity: 0.45,
      depthWrite: false,
      polygonOffset: true,
      polygonOffsetFactor: -4,
    });
    this.shadow = new Mesh(new PlaneGeometry(1.1, 1.1).rotateX(-Math.PI / 2), this.shadowMaterial);
    this.shadow.renderOrder = 1;
  }

  /** Objetos a añadir a la escena. */
  get objects(): Object3D[] {
    return [this.root, this.shadow];
  }

  update(state: PlayerRenderState, body: PlayerBody, events: PlayerFrameEvents, dt: number): void {
    this.time += dt;
    const speed = Math.hypot(body.vx, body.vz);
    const running = body.grounded && !body.sliding;
    const speedFactor = clamp(speed / PLAYER_BASE_STATS.moveSpeed, 0, 1.4);
    const idle = running && speed < 0.3;

    if (events.landed) this.squash = clamp(events.landingSpeed / 25, 0.08, 0.3);
    this.squash = lerp(this.squash, 0, damp(10, dt));
    this.slidePose = lerp(this.slidePose, body.sliding ? 1 : 0, damp(16, dt));
    this.airPose = lerp(this.airPose, !body.grounded && !body.sliding ? 1 : 0, damp(10, dt));

    // Zancada: la frecuencia sube con la velocidad.
    if (running && speed > 0.3) this.phase += dt * (5 + speed * 0.9);
    const swing = running ? Math.sin(this.phase) * 0.75 * Math.min(1, speedFactor) : 0;
    const bob = running ? Math.abs(Math.sin(this.phase)) * 0.06 * Math.min(1, speedFactor) : 0;

    // Piernas y brazos: correr + postura en el aire + postura de tobogán.
    const legSwing = swing * (1 - this.airPose);
    this.legL.rotation.x = lerp(legSwing + this.airPose * 0.5, -1.35, this.slidePose);
    this.legR.rotation.x = lerp(-legSwing - this.airPose * 0.3, -1.35, this.slidePose);
    const wave = Math.sin(this.time * 14) * 0.35;
    const armUp = -2.7 + wave;
    this.armL.rotation.x = lerp(lerp(-swing * 0.9, armUp, this.airPose), armUp, this.slidePose);
    this.armR.rotation.x = lerp(lerp(swing * 0.9, armUp, this.airPose), armUp, this.slidePose);
    this.armL.rotation.z = lerp(0, -0.35, Math.max(this.airPose, this.slidePose));
    this.armR.rotation.z = lerp(0, 0.35, Math.max(this.airPose, this.slidePose));

    // Cuerpo: se inclina al correr y se echa hacia atrás al deslizarse (modo tobogán).
    const lean = running ? -0.12 * Math.min(1, speedFactor) : 0;
    this.upper.rotation.x = lerp(lean, 1.05, this.slidePose);
    this.hips.position.y = HIP_HEIGHT + bob - this.slidePose * 0.3;
    this.hips.scale.set(1 + this.squash * 0.5, 1 - this.squash, 1 + this.squash * 0.5);
    // En reposo respira (y el bolso se mece un poco).
    this.upper.scale.y = idle ? 1 + Math.sin(this.time * 2.4) * 0.018 : 1;
    if (idle) this.armL.rotation.x += Math.sin(this.time * 1.7) * 0.08;

    this.root.position.set(state.x, state.y, state.z);
    this.root.rotation.y = state.facing;

    // Sombra pegada al suelo y orientada según su pendiente; encoge con la altura.
    const height = Math.max(0, state.y - state.groundY);
    const shrink = 1 - clamp(height / 6, 0, 0.65);
    this.shadow.position.set(state.x, state.groundY + 0.03, state.z);
    this.normal.set(state.groundNormal.x, state.groundNormal.y, state.groundNormal.z);
    this.shadow.quaternion.copy(this.quat.setFromUnitVectors(UP, this.normal));
    this.shadow.scale.setScalar(shrink);
    this.shadowMaterial.opacity = 0.45 * shrink;
  }
}
