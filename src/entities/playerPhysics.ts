// Física del jugador: lógica pura (sin Three.js) para poder probarla con tests.
// Movimiento ágil: aceleración rápida, control en el aire, salto con margen
// ("coyote time" y buffer), deslizamiento con impulso que acelera cuesta abajo.
import { clamp, damp, DEG2RAD, lerpAngle, wrapAngle, type Vec3Like } from '../core/math';

export interface SlideTuning {
  /** Velocidad mínima al empezar a deslizarse, como múltiplo de la de correr. */
  entrySpeedMultiplier: number;
  /** Velocidad extra que se suma al empezar a deslizarse (m/s). */
  boost: number;
  /** Por debajo de esta velocidad el deslizamiento termina (m/s). */
  minSpeed: number;
  /** Frenado mientras se desliza (m/s²). */
  friction: number;
  /** Frenado al pulsar hacia atrás mientras se desliza (m/s²). */
  brakeFriction: number;
  /** Multiplicador de la gravedad a lo largo de la pendiente. */
  gravityScale: number;
  /** Giro máximo de la trayectoria (rad/s). */
  turnRate: number;
  /** Tiempo entre impulsos (s). Deslizarse sigue permitido, pero sin impulso. */
  cooldown: number;
  maxSpeed: number;
  /** Velocidad horizontal extra al saltar desde un deslizamiento (m/s). */
  jumpBoost: number;
  /**
   * Los impulsos (al deslizarse o al saltar deslizando) no pasan de esta velocidad,
   * como múltiplo de la de correr. Cuesta abajo la gravedad sí puede superarla.
   */
  boostSpeedCap: number;
}

export interface PlayerTuning {
  radius: number;
  height: number;
  groundAccel: number;
  groundDecel: number;
  airAccel: number;
  /** Frenado cuando vas más rápido que tu velocidad máxima (conserva la inercia). */
  overspeedDecel: number;
  gravity: number;
  fallGravityMultiplier: number;
  /** Gravedad extra al soltar el salto antes de tiempo (salto de altura variable). */
  lowJumpGravityMultiplier: number;
  maxFallSpeed: number;
  jumpVelocity: number;
  coyoteTime: number;
  jumpBuffer: number;
  /** Altura máxima que se sube sin saltar. */
  stepHeight: number;
  /** Pendiente máxima por la que se puede caminar (grados). */
  maxSlopeDeg: number;
  slide: SlideTuning;
}

export interface PlayerIntent {
  /** Dirección deseada en el mundo (longitud 0..1). */
  moveX: number;
  moveZ: number;
  jumpPressed: boolean;
  jumpHeld: boolean;
  slidePressed: boolean;
  slideHeld: boolean;
}

/** Consultas de colisión que la física necesita del mundo. */
export interface PhysicsWorld {
  /** Superficie más alta en (x, z) que no esté por encima de `maxY` (el terreno siempre cuenta). */
  groundHeight(x: number, z: number, maxY: number): number;
  /** Normal de esa misma superficie. */
  groundNormal(x: number, z: number, maxY: number, out: Vec3Like): Vec3Like;
  /** Saca el cuerpo de los obstáculos (troncos, rocas) y anula la velocidad contra ellos. */
  resolveObstacles(body: PlayerBody, radius: number, stepHeight: number): void;
  /** Mantiene el cuerpo dentro del área jugable. */
  constrain(body: PlayerBody): void;
}

export interface PlayerEvents {
  jumped: boolean;
  landed: boolean;
  landingSpeed: number;
  /** Deslizamiento iniciado con impulso. */
  slideBoosted: boolean;
}

export class PlayerBody {
  x = 0;
  /** Altura de los pies. */
  y = 0;
  z = 0;
  vx = 0;
  vy = 0;
  vz = 0;
  grounded = false;
  /** Tocando una pendiente demasiado empinada (resbala). */
  onSteep = false;
  /** Memoria corta de "he tocado una pendiente empinada hace nada" (s). */
  steepContact = 0;
  /** Tiempo atascado entre pendientes empinadas sin llegar a bajar. */
  stuckTime = 0;
  /** Altura de referencia para decidir si sigue bajando o está atascado. */
  stuckRefY = 0;
  readonly normal: Vec3Like = { x: 0, y: 1, z: 0 };
  coyote = 0;
  jumpBuffer = 0;
  sliding = false;
  slideCooldown = 0;
  slideQueued = false;
  /** Orientación del modelo (rad); 0 = mirando hacia -Z. */
  facing = 0;
  readonly events: PlayerEvents = { jumped: false, landed: false, landingSpeed: 0, slideBoosted: false };

  get horizontalSpeed(): number {
    return Math.hypot(this.vx, this.vz);
  }

  placeAt(x: number, y: number, z: number): void {
    this.x = x;
    this.y = y;
    this.z = z;
    this.vx = 0;
    this.vy = 0;
    this.vz = 0;
    this.grounded = true;
    this.onSteep = false;
    this.steepContact = 0;
    this.stuckTime = 0;
    this.stuckRefY = y;
    this.sliding = false;
    this.slideQueued = false;
    this.coyote = 0;
    this.jumpBuffer = 0;
  }
}

const tmpNormal: Vec3Like = { x: 0, y: 1, z: 0 };

/** Cuánto se "recuerda" el contacto con una pendiente empinada (s). */
const STEEP_MEMORY = 0.15;
/** Si en este tiempo no ha bajado STUCK_DROP metros, se considera atascado. */
const STUCK_TIME = 0.35;
const STUCK_DROP = 0.5;
/** Empujón lateral (m/s) al saltar para salir de un atasco, alejándose de la pared. */
const STUCK_JUMP_PUSH = 5;

/** Acerca la velocidad horizontal a (tx, tz) como mucho `maxDelta`. */
function approach(b: PlayerBody, tx: number, tz: number, maxDelta: number): void {
  const dx = tx - b.vx;
  const dz = tz - b.vz;
  const d = Math.hypot(dx, dz);
  if (d <= maxDelta || d < 1e-6) {
    b.vx = tx;
    b.vz = tz;
  } else {
    b.vx += (dx / d) * maxDelta;
    b.vz += (dz / d) * maxDelta;
  }
}

/** Gira la velocidad horizontal hacia (dirX, dirZ) como mucho `maxAngle` radianes. */
function steer(b: PlayerBody, dirX: number, dirZ: number, maxAngle: number): void {
  const speed = Math.hypot(b.vx, b.vz);
  if (speed < 1e-4) return;
  const current = Math.atan2(b.vz, b.vx);
  const diff = clamp(wrapAngle(Math.atan2(dirZ, dirX) - current), -maxAngle, maxAngle);
  const angle = current + diff;
  b.vx = Math.cos(angle) * speed;
  b.vz = Math.sin(angle) * speed;
}

function scaleHorizontal(b: PlayerBody, newSpeed: number): void {
  const speed = Math.hypot(b.vx, b.vz);
  if (speed < 1e-6) return;
  const k = Math.max(0, newSpeed) / speed;
  b.vx *= k;
  b.vz *= k;
}

function startSlide(
  b: PlayerBody,
  dirX: number,
  dirZ: number,
  wishLen: number,
  moveSpeed: number,
  tune: PlayerTuning,
): void {
  const speed = Math.hypot(b.vx, b.vz);
  let dx: number;
  let dz: number;
  if (speed > 0.5) {
    dx = b.vx / speed;
    dz = b.vz / speed;
  } else if (wishLen > 0.1) {
    dx = dirX;
    dz = dirZ;
  } else {
    return; // Parado y sin dirección: no hay hacia dónde deslizarse.
  }
  let newSpeed = speed;
  if (b.slideCooldown <= 0) {
    const cap = moveSpeed * tune.slide.boostSpeedCap;
    newSpeed = Math.max(speed, Math.min(speed + tune.slide.boost, cap), moveSpeed * tune.slide.entrySpeedMultiplier);
    b.slideCooldown = tune.slide.cooldown;
    b.events.slideBoosted = true;
  }
  b.vx = dx * newSpeed;
  b.vz = dz * newSpeed;
  b.sliding = true;
}

/** Avanza la física del jugador un paso de `dt` segundos. */
export function stepPlayer(
  b: PlayerBody,
  intent: PlayerIntent,
  world: PhysicsWorld,
  tune: PlayerTuning,
  moveSpeed: number,
  dt: number,
): void {
  const ev = b.events;
  ev.jumped = false;
  ev.landed = false;
  ev.landingSpeed = 0;
  ev.slideBoosted = false;
  const cosMaxSlope = Math.cos(tune.maxSlopeDeg * DEG2RAD);

  // --- Intención de movimiento
  let wishLen = Math.hypot(intent.moveX, intent.moveZ);
  const dirX = wishLen > 1e-4 ? intent.moveX / wishLen : 0;
  const dirZ = wishLen > 1e-4 ? intent.moveZ / wishLen : 0;
  if (wishLen > 1) wishLen = 1;

  // --- Temporizadores
  b.slideCooldown = Math.max(0, b.slideCooldown - dt);
  b.jumpBuffer = intent.jumpPressed ? tune.jumpBuffer : Math.max(0, b.jumpBuffer - dt);
  b.coyote = b.grounded ? tune.coyoteTime : Math.max(0, b.coyote - dt);
  // Válvula de seguridad: atascado entre pendientes empinadas (p. ej. el fondo
  // de una grieta) sin conseguir bajar → se permite saltar para salir. Si en
  // cambio vas resbalando hacia abajo, no cuenta: así no se puede trepar un
  // acantilado a base de saltos.
  b.steepContact = b.onSteep ? STEEP_MEMORY : Math.max(0, b.steepContact - dt);
  if (b.grounded || b.steepContact <= 0) {
    b.stuckTime = 0;
    b.stuckRefY = b.y;
  } else {
    b.stuckTime += dt;
    if (b.y < b.stuckRefY - STUCK_DROP) {
      b.stuckTime = 0;
      b.stuckRefY = b.y;
    }
  }
  const stuck = b.stuckTime > STUCK_TIME;

  // --- Deslizamiento: inicio, "en cola" desde el aire y fin
  if (!intent.slideHeld) {
    b.slideQueued = false;
    b.sliding = false;
  }
  if (b.grounded) {
    if (!b.sliding && intent.slideHeld && (intent.slidePressed || b.slideQueued)) {
      startSlide(b, dirX, dirZ, wishLen, moveSpeed, tune);
    }
    b.slideQueued = false;
  } else if (b.sliding) {
    // Al salir volando de una rampa se retoma el deslizamiento al aterrizar.
    b.sliding = false;
    b.slideQueued = intent.slideHeld;
  } else if (intent.slidePressed) {
    b.slideQueued = true;
  }

  // --- Aceleración horizontal
  const n = b.normal;
  let speed = Math.hypot(b.vx, b.vz);
  if (b.grounded) {
    if (b.sliding) {
      const braking = wishLen > 0.1 && speed > 0.1 && (dirX * b.vx + dirZ * b.vz) / speed < -0.5;
      scaleHorizontal(b, speed - (braking ? tune.slide.brakeFriction : tune.slide.friction) * dt);
      // La componente de la gravedad paralela al suelo empuja cuesta abajo.
      const g = tune.gravity * tune.slide.gravityScale;
      b.vx += g * n.y * n.x * dt;
      b.vz += g * n.y * n.z * dt;
      if (wishLen > 0.1 && !braking) steer(b, dirX, dirZ, tune.slide.turnRate * dt);
      speed = Math.hypot(b.vx, b.vz);
      if (speed > tune.slide.maxSpeed) scaleHorizontal(b, tune.slide.maxSpeed);
      if (speed < tune.slide.minSpeed) b.sliding = false;
    } else {
      let rate: number;
      if (wishLen < 0.01) rate = tune.groundDecel;
      else if (speed > moveSpeed + 0.1) rate = tune.overspeedDecel;
      else rate = tune.groundAccel;
      approach(b, dirX * moveSpeed * wishLen, dirZ * moveSpeed * wishLen, rate * dt);
    }
  } else if (b.onSteep || b.steepContact > 0) {
    // Resbalando (o rebotando contra una pendiente empinada): la gravedad tira
    // cuesta abajo y la entrada solo empuja hacia los lados o hacia abajo, nunca
    // cuesta arriba. Sin esto, el control aéreo permitiría "flotar" en la pared.
    let ax = dirX * wishLen * tune.airAccel * 0.3;
    let az = dirZ * wishLen * tune.airAccel * 0.3;
    const downLen = Math.hypot(n.x, n.z);
    if (downLen > 1e-4) {
      const along = (ax * n.x + az * n.z) / downLen;
      if (along < 0) {
        ax -= (along * n.x) / downLen;
        az -= (along * n.z) / downLen;
      }
    }
    // (El deslizamiento cuesta abajo sale solo: la gravedad actúa en vertical y
    // el contacto con la pendiente la convierte en movimiento a lo largo de ella.)
    b.vx += ax * dt;
    b.vz += az * dt;
  } else if (wishLen > 0.01) {
    // En el aire no se frena, pero se puede redirigir la inercia.
    const maxAir = Math.max(moveSpeed, speed);
    approach(b, dirX * maxAir * wishLen, dirZ * maxAir * wishLen, tune.airAccel * dt);
  }

  // --- Salto
  if (b.jumpBuffer > 0 && (b.coyote > 0 || stuck)) {
    b.vy = tune.jumpVelocity;
    if (stuck) {
      const hl = Math.hypot(n.x, n.z);
      if (hl > 1e-4) {
        b.vx += (n.x / hl) * STUCK_JUMP_PUSH;
        b.vz += (n.z / hl) * STUCK_JUMP_PUSH;
      }
      b.stuckTime = 0;
    }
    if (b.sliding) {
      const current = Math.hypot(b.vx, b.vz);
      const cap = moveSpeed * tune.slide.boostSpeedCap;
      if (current < cap) scaleHorizontal(b, Math.min(current + tune.slide.jumpBoost, cap));
      b.sliding = false;
    }
    b.grounded = false;
    b.onSteep = false;
    b.coyote = 0;
    b.jumpBuffer = 0;
    ev.jumped = true;
  }

  // --- Gravedad
  if (!b.grounded) {
    let multiplier = 1;
    if (b.vy < 0) multiplier = tune.fallGravityMultiplier;
    else if (!intent.jumpHeld) multiplier = tune.lowJumpGravityMultiplier;
    b.vy = Math.max(b.vy - tune.gravity * multiplier * dt, -tune.maxFallSpeed);
  }

  // --- Movimiento horizontal con bloqueo por paredes y pendientes
  const blocked = (x: number, z: number): boolean => {
    const maxY = b.y + tune.stepHeight;
    const g = world.groundHeight(x, z, maxY);
    if (g > maxY) return true;
    if (g > b.y + 0.02) {
      world.groundNormal(x, z, maxY, tmpNormal);
      if (tmpNormal.y < cosMaxSlope) return true;
    }
    return false;
  };
  let nx = b.x + b.vx * dt;
  let nz = b.z + b.vz * dt;
  if (blocked(nx, nz)) {
    // Quitamos la componente de la velocidad que va "cuesta arriba" y reintentamos.
    world.groundNormal(nx, nz, b.y + tune.stepHeight, tmpNormal);
    const ux = -tmpNormal.x;
    const uz = -tmpNormal.z;
    const ul = Math.hypot(ux, uz);
    if (ul > 1e-4) {
      const into = (b.vx * ux + b.vz * uz) / ul;
      if (into > 0) {
        b.vx -= (into * ux) / ul;
        b.vz -= (into * uz) / ul;
      }
    }
    nx = b.x + b.vx * dt;
    nz = b.z + b.vz * dt;
    if (blocked(nx, nz)) {
      nx = b.x;
      nz = b.z;
    }
  }
  b.x = nx;
  b.z = nz;
  world.resolveObstacles(b, tune.radius, tune.stepHeight);
  world.constrain(b);

  // --- Movimiento vertical y contacto con el suelo
  const wasGrounded = b.grounded;
  b.y += b.vy * dt;
  const maxY = b.y + tune.stepHeight;
  const ground = world.groundHeight(b.x, b.z, maxY);
  world.groundNormal(b.x, b.z, maxY, b.normal);
  const walkable = b.normal.y >= cosMaxSlope;
  if (b.y <= ground) {
    b.y = ground;
    if (walkable) {
      if (!wasGrounded && b.vy < 0) {
        ev.landed = true;
        ev.landingSpeed = -b.vy;
      }
      if (b.vy < 0) b.vy = 0;
    } else {
      // Pendiente empinada: solo se anula la velocidad que va contra la superficie;
      // lo que queda es movimiento paralelo a ella (resbalar hacia abajo).
      const vn = b.vx * b.normal.x + b.vy * b.normal.y + b.vz * b.normal.z;
      if (vn < 0) {
        b.vx -= vn * b.normal.x;
        b.vy -= vn * b.normal.y;
        b.vz -= vn * b.normal.z;
      }
    }
    b.grounded = walkable;
    b.onSteep = !walkable;
  } else {
    // "Pegarse" al suelo al bajar cuestas, salvo en bordes de acantilado.
    const snap = Math.max(0.3, Math.hypot(b.vx, b.vz) * dt * 1.25);
    if (wasGrounded && b.vy <= 0 && walkable && b.y - ground <= snap) {
      b.y = ground;
      b.vy = 0;
      b.grounded = true;
      b.onSteep = false;
    } else {
      b.grounded = false;
      b.onSteep = false;
    }
  }

  // --- Orientación del modelo
  const horizontal = Math.hypot(b.vx, b.vz);
  if (horizontal > 0.6) {
    b.facing = lerpAngle(b.facing, Math.atan2(-b.vx, -b.vz), damp(14, dt));
  } else if (wishLen > 0.1) {
    b.facing = lerpAngle(b.facing, Math.atan2(-dirX, -dirZ), damp(14, dt));
  }
}
