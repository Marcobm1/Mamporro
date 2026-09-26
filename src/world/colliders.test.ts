import { describe, expect, it } from 'vitest';
import { boxCollider, circleCollider, ColliderGrid, isOnTop, pushOut, type PushResult } from './colliders';

const out: PushResult = { dx: 0, dz: 0, nx: 0, nz: 0 };

describe('colisionadores', () => {
  it('un círculo se sale de otro círculo por la línea que une los centros', () => {
    const c = circleCollider(0, 0, 1, 0, 2, false);
    expect(pushOut(c, 1.2, 0, 0.5, out)).toBe(true);
    expect(out.dx).toBeCloseTo(0.3, 6);
    expect(out.dz).toBeCloseTo(0, 6);
    expect(pushOut(c, 2, 0, 0.5, out)).toBe(false);
  });

  it('un círculo se sale de una caja sin girar por la cara más cercana', () => {
    const box = boxCollider(0, 0, 2, 0.5, 0, 0, 3, true);
    // Tocando la cara +Z.
    expect(pushOut(box, 0.3, 0.7, 0.4, out)).toBe(true);
    expect(out.nx).toBeCloseTo(0, 6);
    expect(out.nz).toBeCloseTo(1, 6);
    expect(out.dz).toBeCloseTo(0.2, 6);
    // Lejos de la caja no hay contacto.
    expect(pushOut(box, 0, 1.5, 0.4, out)).toBe(false);
  });

  it('respeta el giro de la caja (convenio de rotation.y de Three)', () => {
    // Caja larga en X girada 90º: pasa a ser larga en Z.
    const box = boxCollider(0, 0, 3, 0.25, Math.PI / 2, 0, 3, true);
    expect(pushOut(box, 0, 2.5, 0.3, out)).toBe(true);
    expect(pushOut(box, 2.5, 0, 0.3, out)).toBe(false);
    expect(isOnTop(box, 0, 2.9)).toBe(true);
    expect(isOnTop(box, 1, 0)).toBe(false);
  });

  it('si el centro queda dentro de la caja, sale por el lado de menor penetración', () => {
    const box = boxCollider(0, 0, 2, 1, 0, 0, 1, true);
    expect(pushOut(box, 1.8, 0.1, 0.3, out)).toBe(true);
    expect(out.nx).toBe(1);
    expect(1.8 + out.dx).toBeCloseTo(2.3, 6);
  });

  it('isOnTop usa la parte central de los cilindros (bordes redondeados)', () => {
    const rock = circleCollider(5, 5, 1, 0, 1, true);
    expect(isOnTop(rock, 5.5, 5)).toBe(true);
    expect(isOnTop(rock, 5.95, 5)).toBe(false);
  });

  it('la rejilla encuentra cajas y círculos igual que la fuerza bruta', () => {
    const colliders = [
      boxCollider(10, 10, 4, 0.3, 0.7, 0, 2, true),
      circleCollider(-20, 5, 1.5, 0, 2, true),
      boxCollider(-40, -40, 1, 1, 0, 0, 1, true),
    ];
    const grid = new ColliderGrid(colliders, 100);
    const found: number[] = [];
    for (let n = 0; n < 400; n++) {
      const x = ((n * 37) % 120) - 60;
      const z = ((n * 53) % 120) - 60;
      const r = 0.5;
      const result = new Set(grid.query(x, z, r, found));
      colliders.forEach((c, i) => {
        if (pushOut(c, x, z, r, out)) expect(result.has(i)).toBe(true);
      });
    }
  });
});
