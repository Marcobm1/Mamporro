import { describe, expect, it } from 'vitest';
import { DEG2RAD, type Vec3Like } from '../core/math';
import { PLAYER_BASE_STATS, PLAYER_TUNING } from '../data/config';
import { brakeHorizontal, PlayerBody, stepPlayer, type PhysicsWorld, type PlayerIntent } from './playerPhysics';

const DT = 1 / 60;
const SPEED = PLAYER_BASE_STATS.moveSpeed;

/** Mundo de prueba definido por una función de altura (normal por diferencias finitas). */
class TestWorld implements PhysicsWorld {
  constructor(private readonly height: (x: number, z: number) => number) {}

  groundHeight(x: number, z: number): number {
    return this.height(x, z);
  }

  groundNormal(x: number, z: number, _maxY: number, out: Vec3Like): Vec3Like {
    const d = 0.001;
    const dx = (this.height(x + d, z) - this.height(x - d, z)) / (2 * d);
    const dz = (this.height(x, z + d) - this.height(x, z - d)) / (2 * d);
    const inv = 1 / Math.sqrt(dx * dx + 1 + dz * dz);
    out.x = -dx * inv;
    out.y = inv;
    out.z = -dz * inv;
    return out;
  }

  resolveObstacles(): void {}
  constrain(): void {}
}

const flat = new TestWorld(() => 0);

function intent(partial: Partial<PlayerIntent> = {}): PlayerIntent {
  return {
    moveX: 0,
    moveZ: 0,
    jumpPressed: false,
    jumpHeld: false,
    slidePressed: false,
    slideHeld: false,
    ...partial,
  };
}

function run(
  body: PlayerBody,
  world: PhysicsWorld,
  seconds: number,
  input: PlayerIntent | ((tick: number) => PlayerIntent),
  onTick?: (tick: number) => void,
): void {
  const ticks = Math.round(seconds / DT);
  for (let t = 0; t < ticks; t++) {
    stepPlayer(body, typeof input === 'function' ? input(t) : input, world, PLAYER_TUNING, SPEED, DT);
    onTick?.(t);
  }
}

function spawn(world: PhysicsWorld, x = 0, z = 0): PlayerBody {
  const body = new PlayerBody();
  body.placeAt(x, world.groundHeight(x, z, Infinity), z);
  return body;
}

describe('física del jugador', () => {
  it('alcanza la velocidad máxima en menos de 0,25 s', () => {
    const body = spawn(flat);
    run(body, flat, 0.25, intent({ moveX: 1 }));
    expect(body.horizontalSpeed).toBeCloseTo(SPEED, 1);
  });

  it('frena en seco al soltar las teclas', () => {
    const body = spawn(flat);
    run(body, flat, 0.5, intent({ moveX: 1 }));
    run(body, flat, 0.2, intent());
    expect(body.horizontalSpeed).toBeLessThan(0.01);
  });

  it('el salto completo llega a la altura teórica v²/2g', () => {
    const body = spawn(flat);
    let apex = 0;
    run(body, flat, 1.2, (t) => intent({ jumpPressed: t === 0, jumpHeld: true }), () => {
      apex = Math.max(apex, body.y);
    });
    const expected = PLAYER_TUNING.jumpVelocity ** 2 / (2 * PLAYER_TUNING.gravity);
    expect(apex).toBeGreaterThan(expected * 0.93);
    expect(apex).toBeLessThan(expected * 1.03);
    expect(body.grounded).toBe(true);
  });

  it('soltar el salto pronto da un salto más bajo', () => {
    const body = spawn(flat);
    let apex = 0;
    run(body, flat, 1, (t) => intent({ jumpPressed: t === 0, jumpHeld: t < 2 }), () => {
      apex = Math.max(apex, body.y);
    });
    expect(apex).toBeLessThan(1);
    expect(apex).toBeGreaterThan(0.4);
  });

  it('coyote time: se puede saltar justo después de salir de un borde', () => {
    const cliff = new TestWorld((x) => (x < 1 ? 0 : -6));
    const body = spawn(cliff);
    let leftAt = -1;
    let jumped = false;
    run(
      body,
      cliff,
      1,
      (t) => intent({ moveX: 1, jumpPressed: leftAt >= 0 && t === leftAt + 3, jumpHeld: true }),
      (t) => {
        if (leftAt < 0 && !body.grounded) leftAt = t;
        if (body.events.jumped) jumped = true;
      },
    );
    expect(leftAt).toBeGreaterThan(0);
    expect(jumped).toBe(true);
  });

  it('jump buffer: pulsar saltar un poco antes de aterrizar también salta', () => {
    const body = spawn(flat);
    body.y = 3;
    body.grounded = false;
    let landedTick = -1;
    let jumpedTick = -1;
    // Caer hasta estar a punto de tocar el suelo y pulsar en ese momento.
    let pressTick = -1;
    run(
      body,
      flat,
      1.5,
      (t) => {
        if (pressTick < 0 && body.vy < 0 && body.y < 0.4) pressTick = t;
        return intent({ jumpPressed: t === pressTick, jumpHeld: true });
      },
      (t) => {
        if (landedTick < 0 && body.events.landed) landedTick = t;
        if (jumpedTick < 0 && body.events.jumped) jumpedTick = t;
      },
    );
    expect(pressTick).toBeGreaterThan(0);
    expect(jumpedTick).toBeGreaterThan(pressTick);
    expect(jumpedTick - landedTick).toBeLessThanOrEqual(1);
  });

  it('deslizarse da un impulso por encima de la velocidad de carrera', () => {
    const body = spawn(flat);
    run(body, flat, 0.5, intent({ moveX: 1 }));
    run(body, flat, DT, intent({ moveX: 1, slidePressed: true, slideHeld: true }));
    expect(body.sliding).toBe(true);
    expect(body.events.slideBoosted).toBe(true);
    // En el mismo tick ya se aplica un poco de fricción.
    const expected = SPEED * PLAYER_TUNING.slide.entrySpeedMultiplier - PLAYER_TUNING.slide.friction * DT;
    expect(body.horizontalSpeed).toBeGreaterThanOrEqual(expected - 1e-6);
  });

  it('el impulso del deslizamiento tiene tiempo de recarga', () => {
    const body = spawn(flat);
    run(body, flat, 0.5, intent({ moveX: 1 }));
    run(body, flat, DT, intent({ moveX: 1, slidePressed: true, slideHeld: true }));
    run(body, flat, 0.1, intent({ moveX: 1 }));
    run(body, flat, DT, intent({ moveX: 1, slidePressed: true, slideHeld: true }));
    expect(body.events.slideBoosted).toBe(false);
  });

  it('cuesta abajo el deslizamiento acelera; en llano se acaba parando', () => {
    const angle = 25 * DEG2RAD;
    const downhill = new TestWorld((x) => -x * Math.tan(angle)); // Baja hacia +x.
    const body = spawn(downhill);
    run(body, downhill, 0.4, intent({ moveX: 1 }));
    run(body, downhill, DT, intent({ moveX: 1, slidePressed: true, slideHeld: true }));
    const start = body.horizontalSpeed;
    run(body, downhill, 1, intent({ moveX: 1, slideHeld: true }));
    expect(body.sliding).toBe(true);
    expect(body.horizontalSpeed).toBeGreaterThan(start + 5);

    const onFlat = spawn(flat);
    run(onFlat, flat, 0.4, intent({ moveX: 1 }));
    run(onFlat, flat, DT, intent({ moveX: 1, slidePressed: true, slideHeld: true }));
    run(onFlat, flat, 5, intent({ moveX: 1, slideHeld: true }));
    expect(onFlat.sliding).toBe(false);
  });

  it('se puede subir andando una cuesta de 30º', () => {
    const angle = 30 * DEG2RAD;
    const uphill = new TestWorld((x) => Math.max(0, x) * Math.tan(angle));
    const body = spawn(uphill, -2);
    run(body, uphill, 2, intent({ moveX: 1 }));
    expect(body.x).toBeGreaterThan(8);
    expect(body.y).toBeGreaterThan(4);
  });

  it('no se puede subir un acantilado de 70º (ni saltando contra él)', () => {
    const angle = 70 * DEG2RAD;
    const cliff = new TestWorld((x) => Math.max(0, x - 5) * Math.tan(angle));
    const body = spawn(cliff);
    let maxY = 0;
    run(body, cliff, 4, (t) => intent({ moveX: 1, jumpPressed: t % 25 === 0, jumpHeld: true }), () => {
      maxY = Math.max(maxY, body.y);
    });
    const jumpApex = PLAYER_TUNING.jumpVelocity ** 2 / (2 * PLAYER_TUNING.gravity);
    expect(maxY).toBeLessThan(jumpApex + 0.3);
    expect(body.x).toBeLessThan(6);
  });

  it('una pared vertical más alta que un escalón bloquea el paso', () => {
    const wall = new TestWorld((x) => (x > 5 ? 3 : 0));
    const body = spawn(wall);
    run(body, wall, 2, intent({ moveX: 1 }));
    expect(body.x).toBeLessThanOrEqual(5);
    expect(body.y).toBe(0);
  });

  it('un escalón bajo se sube sin saltar', () => {
    const step = new TestWorld((x) => (x > 3 ? 0.3 : 0));
    const body = spawn(step);
    run(body, step, 1.5, intent({ moveX: 1 }));
    expect(body.x).toBeGreaterThan(5);
    expect(body.y).toBeCloseTo(0.3, 5);
  });

  it('en el aire se conserva la inercia del deslizamiento', () => {
    const body = spawn(flat);
    run(body, flat, 0.4, intent({ moveX: 1 }));
    run(body, flat, DT, intent({ moveX: 1, slidePressed: true, slideHeld: true }));
    run(body, flat, DT, intent({ moveX: 1, slideHeld: true, jumpPressed: true, jumpHeld: true }));
    const launch = body.horizontalSpeed;
    run(body, flat, 0.3, intent({ moveX: 1, jumpHeld: true }));
    expect(body.grounded).toBe(false);
    expect(body.horizontalSpeed).toBeCloseTo(launch, 5);
    expect(launch).toBeGreaterThan(SPEED * 1.4);
  });

  it('encadenar saltos y deslizamientos en llano no acelera sin límite', () => {
    const body = spawn(flat);
    let maxSpeed = 0;
    // Salta nada más tocar el suelo y vuelve a pulsar deslizarse en el aire, una y otra vez.
    run(
      body,
      flat,
      8,
      (t) =>
        intent({
          moveX: 1,
          jumpPressed: body.grounded && t % 2 === 0,
          jumpHeld: true,
          slidePressed: !body.grounded && t % 10 === 0,
          slideHeld: true,
        }),
      () => {
        maxSpeed = Math.max(maxSpeed, body.horizontalSpeed);
      },
    );
    expect(maxSpeed).toBeLessThanOrEqual(SPEED * PLAYER_TUNING.slide.boostSpeedCap + 1e-6);
  });

  it('cuesta abajo la gravedad sí puede superar el límite de los impulsos', () => {
    const angle = 30 * DEG2RAD;
    const downhill = new TestWorld((x) => -x * Math.tan(angle));
    const body = spawn(downhill);
    run(body, downhill, 0.4, intent({ moveX: 1 }));
    run(body, downhill, DT, intent({ moveX: 1, slidePressed: true, slideHeld: true }));
    run(body, downhill, 2.5, intent({ moveX: 1, slideHeld: true }));
    expect(body.horizontalSpeed).toBeGreaterThan(SPEED * PLAYER_TUNING.slide.boostSpeedCap);
  });

  it('si se atasca en el fondo de una grieta empinada puede saltar para salir', () => {
    const angle = 70 * DEG2RAD;
    const crevice = new TestWorld((x) => Math.abs(x) * Math.tan(angle));
    const body = spawn(crevice, 0.05);
    let jumped = false;
    run(body, crevice, 1, (t) => intent({ jumpPressed: t === 40, jumpHeld: true }), () => {
      if (body.events.jumped) jumped = true;
    });
    expect(jumped).toBe(true);
  });
});

describe('frenado externo', () => {
  it('brakeHorizontal frena hacia el límite sin pasarse, sin girar y sin acelerar nunca', () => {
    const b = new PlayerBody();
    b.vx = 6;
    b.vz = 8; // 10 m/s
    brakeHorizontal(b, 5, 2);
    expect(Math.hypot(b.vx, b.vz)).toBeCloseTo(8);
    expect(b.vz / b.vx).toBeCloseTo(8 / 6);
    brakeHorizontal(b, 5, 100);
    expect(Math.hypot(b.vx, b.vz)).toBeCloseTo(5);
    // Si ya va más despacio que el límite, no hace nada.
    brakeHorizontal(b, 7, 100);
    expect(Math.hypot(b.vx, b.vz)).toBeCloseTo(5);
  });
});
