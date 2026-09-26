import { describe, expect, it } from 'vitest';
import { Run, type RunEffects } from '../core/Run';
import { CHARACTERS } from '../data/characters';
import { CROWD_CONFIG, PLAYER_BASE_STATS, PLAYER_TUNING } from '../data/config';
import { enemyTypeIndex } from '../data/enemies';
import { AURA_BASE_RADIUS, WEAPONS } from '../data/weapons';
import { PlayerBody, type PlayerIntent } from '../entities/playerPhysics';
import { generateWorldData } from '../world/World';
import { stepPlayerInCrowd } from './crowd';
import { EnemySystem } from './EnemySystem';
import { GemSystem } from './GemSystem';

const DT = 1 / 60;
const world = generateWorldData('COMBAT-TEST');
const collision = world.collision;
const PELUSA = enemyTypeIndex('pelusa');

function standingPlayer(): PlayerBody {
  const body = new PlayerBody();
  body.placeAt(0, world.heightfield.heightAt(0, 0), 0);
  return body;
}

function newRun(fx?: Partial<RunEffects>): Run {
  const effects: RunEffects = {
    damageNumber: () => {},
    enemyKilled: () => {},
    enemySpawned: () => {},
    playerHit: () => {},
    levelUp: () => {},
    arcSwing: () => {},
    chainZap: () => {},
    ...fx,
  };
  return new Run(collision, 'COMBAT-TEST', CHARACTERS.remedios, effects);
}

describe('enemigos', () => {
  it('persiguen al jugador', () => {
    const enemies = new EnemySystem(64, world.heightfield.size);
    enemies.spawn(PELUSA, 20, 0, 3, 1, 1);
    enemies.rebuildGrid();
    const target = { x: 0, y: world.heightfield.heightAt(0, 0), z: 0, radius: 0.45, vx: 0, vz: 0 };
    for (let t = 0; t < 180; t++) enemies.update(DT, target, collision);
    expect(Math.hypot(enemies.x[0] as number, enemies.z[0] as number)).toBeLessThan(10);
  });

  it('se separan entre sí en lugar de amontonarse', () => {
    const enemies = new EnemySystem(64, world.heightfield.size);
    for (let i = 0; i < 20; i++) enemies.spawn(PELUSA, 4 + (i % 5) * 0.05, 0, (i / 5) * 0.05, 1, 1);
    enemies.rebuildGrid();
    const far = { x: 200, y: 0, z: 200, radius: 0.45, vx: 0, vz: 0 };
    for (let t = 0; t < 90; t++) enemies.update(DT, far, collision);
    let minDist = Infinity;
    for (let i = 0; i < enemies.count; i++) {
      for (let j = i + 1; j < enemies.count; j++) {
        minDist = Math.min(minDist, Math.hypot((enemies.x[i] as number) - (enemies.x[j] as number), (enemies.z[i] as number) - (enemies.z[j] as number)));
      }
    }
    expect(minDist).toBeGreaterThan(0.5);
  });

  it('golpean por contacto y respetan su tiempo de recarga', () => {
    const enemies = new EnemySystem(8, world.heightfield.size);
    const ground = world.heightfield.heightAt(0, 0);
    enemies.spawn(PELUSA, 0.6, ground, 0, 1, 1);
    enemies.rebuildGrid();
    const target = { x: 0, y: ground, z: 0, radius: 0.45, vx: 0, vz: 0 };
    expect(enemies.update(DT, target, collision)).toBeGreaterThan(0);
    expect(enemies.update(DT, target, collision)).toBe(0);
  });

  it('frenan al jugador que los empuja de frente; no si se aleja, pasa de lado o está quieto', () => {
    const ground = world.heightfield.heightAt(0, 0);
    const pressureWith = (vx: number, vz: number): number => {
      const enemies = new EnemySystem(8, world.heightfield.size);
      enemies.spawn(PELUSA, 0.6, ground, 0, 1, 1); // Pegada al jugador, hacia +x.
      enemies.rebuildGrid();
      enemies.update(DT, { x: 0, y: ground, z: 0, radius: 0.45, vx, vz }, collision);
      return enemies.playerPressure;
    };
    expect(pressureWith(9, 0)).toBeCloseTo(1); // De frente: una pelusa (masa 1).
    expect(pressureWith(-9, 0)).toBe(0); // Alejándose.
    expect(pressureWith(0, 9)).toBeLessThan(0.05); // De lado.
    expect(pressureWith(0, 0)).toBe(0); // Quieto.
  });

  it('subirse a una roca o a unas cajas no protege; a lo alto de un muro, sí', () => {
    const ground = world.heightfield.heightAt(0, 0);
    const contactAt = (height: number): number => {
      const enemies = new EnemySystem(8, world.heightfield.size);
      enemies.spawn(PELUSA, 0.6, ground, 0, 1, 1);
      enemies.rebuildGrid();
      return enemies.update(DT, { x: 0, y: ground + height, z: 0, radius: 0.45, vx: 0, vz: 0 }, collision);
    };
    expect(contactAt(1.2)).toBeGreaterThan(0);
    expect(contactAt(3)).toBe(0);
  });

  it('eliminar en mitad del array conserva los datos del resto (ids estables)', () => {
    const enemies = new EnemySystem(8, world.heightfield.size);
    for (let i = 0; i < 4; i++) enemies.spawn(PELUSA, i * 3, 0, 0, 1, 1);
    const lastId = enemies.id[3];
    enemies.hp[1] = 0;
    const dead: number[] = [];
    enemies.flushDead((i) => dead.push(enemies.id[i] as number));
    expect(dead).toEqual([2]);
    expect(enemies.count).toBe(3);
    expect(enemies.id[1]).toBe(lastId);
    expect(enemies.x[1]).toBe(9);
  });
});

describe('gemas', () => {
  it('se recogen al acercarse y suman su valor', () => {
    const gems = new GemSystem(10);
    gems.spawn(2, 0, 0, 3);
    gems.spawn(40, 0, 0, 5);
    let total = 0;
    for (let t = 0; t < 120; t++) total += gems.update(DT, 0, 0, 0, 3.2);
    expect(total).toBe(3);
    expect(gems.count).toBe(1);
  });

  it('si no caben más, se fusionan sin perder experiencia', () => {
    const gems = new GemSystem(5);
    for (let i = 0; i < 25; i++) gems.spawn(i, 0, 0, 2);
    expect(gems.count).toBe(5);
    let sum = 0;
    for (let i = 0; i < gems.count; i++) sum += gems.value[i] as number;
    expect(sum).toBe(50);
  });
});

describe('partida', () => {
  it('la chancla sale hacia el enemigo más cercano y perfora exactamente uno extra', () => {
    const run = newRun();
    const body = standingPlayer();
    const y = world.heightfield.heightAt(0, 0);
    // Tres pelusas en fila delante del jugador. El primer disparo sale a los 0,3 s y
    // el segundo a los 1,15 s: al segundo 1 solo ha volado una chancla (perforación 1).
    for (let k = 0; k < 3; k++) run.enemies.spawn(PELUSA, 0, y, -6 - k * 1.2, 50, 1);
    run.enemies.rebuildGrid();
    for (let t = 0; t < 60; t++) run.update(DT, body, 0);
    const chancla = run.weapons[0];
    expect(chancla?.def.id).toBe('chancla');
    const hurt = Array.from({ length: run.enemies.count }, (_, i) => (run.enemies.hp[i] as number) < (run.enemies.maxHp[i] as number)).filter(Boolean).length;
    expect(hurt).toBe(2);
    expect(chancla?.totalDamage).toBeGreaterThan(0);
  });

  it('el aura daña a los enemigos cercanos y no a los lejanos', () => {
    const run = newRun();
    run.weapons.length = 0; // Solo el aura, para aislar su efecto.
    const aura = run.addWeapon('naftalina');
    expect(aura?.slot).toBe(0);
    const body = standingPlayer();
    const y = world.heightfield.heightAt(0, 0);
    const near = run.enemies.spawn(PELUSA, AURA_BASE_RADIUS - 1, y, 0, 100, 1);
    const far = run.enemies.spawn(PELUSA, 0, y, 25, 100, 1);
    const nearId = run.enemies.id[near];
    const farId = run.enemies.id[far];
    run.enemies.rebuildGrid();
    const indexOf = (id: number | undefined): number => Array.from(run.enemies.id.subarray(0, run.enemies.count)).indexOf(id ?? -1);
    let maxFlash = 0;
    for (let t = 0; t < 40; t++) {
      run.update(DT, body, 0);
      maxFlash = Math.max(maxFlash, run.enemies.flash[indexOf(nearId)] as number);
    }
    const n = indexOf(nearId);
    const f = indexOf(farId);
    expect(run.enemies.hp[n]).toBeLessThan(run.enemies.maxHp[n] as number);
    expect(run.enemies.hp[f]).toBe(run.enemies.maxHp[f]);
    expect(aura?.totalDamage).toBeGreaterThan(0);
    // El destello del aura es suave (golpea a muchos a la vez) y más flojo que el de la chancla.
    expect(maxFlash).toBeGreaterThan(0);
    expect(maxFlash).toBeLessThanOrEqual(WEAPONS.naftalina.hitFlash);
    expect(WEAPONS.naftalina.hitFlash).toBeLessThan(WEAPONS.chancla.hitFlash);
  });

  it('atravesar una horda frena un poco, pero no encierra (ni rodeada)', () => {
    const forward: PlayerIntent = { moveX: 1, moveZ: 0, jumpPressed: false, jumpHeld: false, slidePressed: false, slideHeld: false };
    const spawnAt = (run: Run, x: number, z: number): void => {
      run.enemies.spawn(PELUSA, x, world.heightfield.heightAt(x, z), z, 1, 1);
    };
    /** Corre hacia +x desde `startX` hasta x = 8 y mide cuánto tarda y cuánto la frenan. */
    const cross = (horde: 'none' | 'wall' | 'ring', startX: number) => {
      const run = newRun();
      run.weapons.length = 0; // Sin armas: la horda no se deshace sola.
      run.invincible = true;
      const body = new PlayerBody();
      body.placeAt(startX, world.heightfield.heightAt(startX, 0), 0);
      if (horde === 'wall') {
        // Un muro de pelusas de 3 filas cruzado en el camino.
        for (let row = 0; row < 3; row++) for (let k = -6; k <= 6; k++) spawnAt(run, row * 1.05, k * 1.05);
      } else if (horde === 'ring') {
        // Rodeada por tres anillos de pelusas.
        for (const r of [1.1, 2.15, 3.2]) {
          const n = Math.floor((2 * Math.PI * r) / 1.05);
          for (let k = 0; k < n; k++) spawnAt(run, startX + Math.cos((k / n) * 2 * Math.PI) * r, Math.sin((k / n) * 2 * Math.PI) * r);
        }
      }
      run.enemies.rebuildGrid();
      let time = 0;
      let minSpeed = Infinity;
      let maxSlow = 0;
      while (body.x < 8 && time < 10) {
        stepPlayerInCrowd(body, forward, collision, PLAYER_TUNING, PLAYER_BASE_STATS.moveSpeed, run.crowdSlow, DT);
        run.update(DT, body, 0);
        time += DT;
        if (time > 0.3) minSpeed = Math.min(minSpeed, body.horizontalSpeed);
        maxSlow = Math.max(maxSlow, run.crowdSlow);
      }
      return { time, minSpeed, maxSlow };
    };
    const floor = PLAYER_BASE_STATS.moveSpeed * (1 - CROWD_CONFIG.maxSlow) - 0.2;
    for (const startX of [-8, 0]) {
      const free = cross('none', startX);
      const horde = cross(startX < 0 ? 'wall' : 'ring', startX);
      console.log(`[horda] ${startX < 0 ? 'muro' : 'rodeada'}: libre ${free.time.toFixed(2)} s · con horda ${horde.time.toFixed(2)} s · velocidad mínima ${horde.minSpeed.toFixed(2)} m/s · frenado máximo ${(horde.maxSlow * 100).toFixed(0)} %`);
      expect(free.maxSlow).toBe(0);
      expect(horde.maxSlow).toBeGreaterThan(0.3); // Se nota...
      expect(horde.time).toBeGreaterThan(free.time * 1.05); // ...tarda más...
      expect(horde.time).toBeLessThan(free.time * 1.6); // ...pero se sale.
      expect(horde.minSpeed).toBeGreaterThan(floor); // Nunca frena más que el tope.
    }
  });

  it('una partida simulada avanza: mata, sube de nivel y elige cartas', () => {
    const levels: number[] = [];
    const run = newRun({ levelUp: (l) => levels.push(l) });
    run.invincible = true;
    const body = standingPlayer();
    let chosen = 0;
    for (let t = 0; t < 60 * 60; t++) {
      run.update(DT, body, t * 0.01);
      // Como en el juego: con una subida pendiente se elige carta (aquí, un arma nueva si la hay).
      while (run.openLevelUp() || run.offer) {
        const offer = run.offer ?? [];
        run.choose(Math.max(0, offer.findIndex((c) => c.kind === 'newWeapon')));
        chosen++;
      }
    }
    expect(run.kills).toBeGreaterThan(20);
    expect(run.level).toBeGreaterThanOrEqual(2);
    expect(levels[0]).toBe(2);
    expect(chosen).toBe(run.level - 1);
    expect(run.pendingLevelUps).toBe(0);
    expect(run.weapons.length).toBeGreaterThanOrEqual(2);
    for (let i = 0; i < run.enemies.count; i++) expect(Number.isFinite(run.enemies.x[i] as number)).toBe(true);
  });

  it('sin invencibilidad, los enemigos acaban con una abuela quieta', () => {
    let hits = 0;
    const run = newRun({ playerHit: () => hits++ });
    const body = standingPlayer();
    for (let t = 0; t < 60 * 60 * 4 && !run.dead; t++) run.update(DT, body, 0);
    expect(hits).toBeGreaterThan(0);
    expect(run.dead).toBe(true);
    expect(run.hp).toBe(0);
  });

  it('rendimiento: la lógica con 500 enemigos cabe de sobra en un tick de 16 ms', () => {
    const run = newRun();
    run.invincible = true;
    const body = standingPlayer();
    run.addWeapon('naftalina');
    run.debugSpawn(500);
    for (let t = 0; t < 30; t++) run.update(DT, body, 0); // Calentamiento del JIT.
    const ticks = 240;
    const start = performance.now();
    for (let t = 0; t < ticks; t++) run.update(DT, body, t * 0.01);
    const perTick = (performance.now() - start) / ticks;
    console.log(`[bench] lógica con ~${run.enemies.count} enemigos: ${perTick.toFixed(3)} ms por tick`);
    expect(run.enemies.count).toBeGreaterThan(300);
    expect(perTick).toBeLessThan(8);
  });

  it('rendimiento: las 4 armas nuevas mejoradas, con 500 enemigos y la abuela corriendo', () => {
    const run = newRun();
    run.invincible = true;
    run.weapons.length = 0;
    for (const id of ['barra', 'dentaduras', 'jersey', 'fregona'] as const) run.debugAddWeapon(id);
    for (let k = 0; k < 3; k++) run.debugAddTome('projectiles');
    run.debugAddTome('area');
    // 500 pelusas repartidas en espiral alrededor del recorrido (a 2–16 m del centro).
    for (let k = 0; k < 500; k++) {
      const a = k * 2.39996;
      const r = 2 + (14 * k) / 500;
      const x = Math.cos(a) * r;
      const z = Math.sin(a) * r;
      run.enemies.spawn(PELUSA, x, world.heightfield.heightAt(x, z), z, 20, 1);
    }
    run.enemies.rebuildGrid();
    const body = standingPlayer();
    const step = (t: number): void => {
      // En círculo, para que la fregona vaya dejando charcos.
      const a = t * 0.02;
      body.x = Math.cos(a) * 6;
      body.z = Math.sin(a) * 6;
      body.y = world.heightfield.heightAt(body.x, body.z);
      body.vx = -Math.sin(a) * 7;
      body.vz = Math.cos(a) * 7;
      body.grounded = true;
      run.update(DT, body, 0);
    };
    for (let t = 0; t < 30; t++) step(t);
    const ticks = 240;
    const start = performance.now();
    for (let t = 30; t < 30 + ticks; t++) step(t);
    const perTick = (performance.now() - start) / ticks;
    console.log(`[bench] 4 armas nuevas con ~${run.enemies.count} enemigos: ${perTick.toFixed(3)} ms por tick`);
    console.log(`[bench] daño: ${run.weapons.map((w) => `${w.def.id}=${Math.round(w.totalDamage)}`).join(" ")}`);
    expect(run.weapons.every((w) => w.totalDamage > 0)).toBe(true);
    expect(perTick).toBeLessThan(8);
  });
});
