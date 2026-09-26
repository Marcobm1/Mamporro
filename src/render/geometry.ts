// Utilidades para construir modelos low-poly a partir de primitivas con color de vértice.
import { BufferAttribute, BufferGeometry, Color } from 'three';
import type { Rng } from '../core/rng';

/**
 * Devuelve una copia no indexada con color de vértice uniforme y solo los
 * atributos `position`, `normal` y `color` (para poder unirla con otras).
 */
export function colored(geometry: BufferGeometry, hex: number): BufferGeometry {
  const g = geometry.index ? geometry.toNonIndexed() : geometry.clone();
  if (!g.getAttribute('normal')) g.computeVertexNormals();
  const count = g.getAttribute('position').count;
  const color = new Color(hex);
  const colors = new Float32Array(count * 3);
  for (let i = 0; i < count; i++) {
    colors[i * 3] = color.r;
    colors[i * 3 + 1] = color.g;
    colors[i * 3 + 2] = color.b;
  }
  g.setAttribute('color', new BufferAttribute(colors, 3));
  for (const name of Object.keys(g.attributes)) {
    if (name !== 'position' && name !== 'normal' && name !== 'color') g.deleteAttribute(name);
  }
  return g;
}

/** Une geometrías generadas con `colored` en una sola (una única llamada de dibujo). */
export function mergeColored(geometries: readonly BufferGeometry[]): BufferGeometry {
  let total = 0;
  for (const g of geometries) total += g.getAttribute('position').count;
  const position = new Float32Array(total * 3);
  const normal = new Float32Array(total * 3);
  const color = new Float32Array(total * 3);
  let offset = 0;
  for (const g of geometries) {
    const count = g.getAttribute('position').count;
    position.set(g.getAttribute('position').array, offset * 3);
    normal.set(g.getAttribute('normal').array, offset * 3);
    color.set(g.getAttribute('color').array, offset * 3);
    offset += count;
  }
  const merged = new BufferGeometry();
  merged.setAttribute('position', new BufferAttribute(position, 3));
  merged.setAttribute('normal', new BufferAttribute(normal, 3));
  merged.setAttribute('color', new BufferAttribute(color, 3));
  merged.computeBoundingSphere();
  return merged;
}

/**
 * Desplaza al azar los vértices (moviendo juntos los que coinciden en posición
 * para no abrir huecos). Da formas orgánicas a rocas y copas de árboles.
 */
export function jitterVertices(geometry: BufferGeometry, rng: Rng, amount: number): BufferGeometry {
  const pos = geometry.getAttribute('position');
  const offsets = new Map<string, [number, number, number]>();
  for (let i = 0; i < pos.count; i++) {
    const x = pos.getX(i);
    const y = pos.getY(i);
    const z = pos.getZ(i);
    const key = `${x.toFixed(3)},${y.toFixed(3)},${z.toFixed(3)}`;
    let o = offsets.get(key);
    if (!o) {
      o = [rng.range(-amount, amount), rng.range(-amount, amount), rng.range(-amount, amount)];
      offsets.set(key, o);
    }
    pos.setXYZ(i, x + o[0], y + o[1], z + o[2]);
  }
  pos.needsUpdate = true;
  geometry.computeVertexNormals();
  return geometry;
}
