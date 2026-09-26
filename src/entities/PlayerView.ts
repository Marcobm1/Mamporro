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
  return mergeColored([
    colored(new BoxGeometry(0.11, 0.4, 0.11).translate(side * 0.1, -0.2, 0), PALETTE.tights),
    colored(new BoxGeometry(0.15, 0.08, 0.26).translate(side * 0.1, -0.42, -0.04), PALETTE.slipper),
  ]);
}

function buildArm(side: number, withChancla: boolean): Mesh['geometry'] {
  const parts = [
    colored(new BoxGeometry(0.09, 0.36, 0.09).translate(0, -0.18, 0), PALETTE.cardigan),
    colored(new BoxGeometry(0.08, 0.08, 0.08).translate(0, -0.4, 0), PALETTE.skin),
  ];
  if (withChancla) {
    parts.push(colored(new BoxGeometry(0.1, 0.03, 0.26).translate(0, -0.45, -0.08), PALETTE.chancla));
  }
  const g = mergeColored(parts);
  g.translate(side * 0.25, 0, 0);
  return g;
}

function buildUpperBody(): Mesh['geometry'] {
  return mergeColored([
    // Vestido y chaqueta de punto
    colored(new CylinderGeometry(0.2, 0.36, 0.56, 7).translate(0, 0.2, 0), PALETTE.dress),
    colored(new CylinderGeometry(0.19, 0.22, 0.34, 7).translate(0, 0.62, 0), PALETTE.cardigan),
    // Cabeza, pelo y moño
    colored(new IcosahedronGeometry(0.17, 0).translate(0, 0.92, 0), PALETTE.skin),
    colored(new SphereGeometry(0.18, 7, 4, 0, Math.PI * 2, 0, Math.PI * 0.5).translate(0, 0.95, 0.02), PALETTE.hairGrey),
    colored(new IcosahedronGeometry(0.1, 0).translate(0, 1.1, 0.1), PALETTE.hairGrey),
    // Gafas y nariz (miran hacia -Z)
    colored(new BoxGeometry(0.24, 0.05, 0.03).translate(0, 0.94, -0.16), PALETTE.glasses),
    colored(new BoxGeometry(0.05, 0.06, 0.06).translate(0, 0.88, -0.18), PALETTE.skin),
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
    this.armL = new Mesh(buildArm(-1, false), material);
    this.armR = new Mesh(buildArm(1, true), material);
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
    const speedFactor = clamp(speed / 8, 0, 1.4);

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
