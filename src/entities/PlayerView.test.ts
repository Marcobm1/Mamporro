import { GreaterDepth, Mesh, MeshBasicMaterial, Texture } from 'three';
import { describe, expect, it } from 'vitest';
import { RENDER_ORDER } from '../render/renderOrder';
import { PlayerView } from './PlayerView';

describe('modelo de Doña Remedios', () => {
  it('cada pieza lleva una silueta que comparte su geometría y solo se dibuja donde algo la tapa', () => {
    const view = new PlayerView(new Texture());
    const parts: Mesh[] = [];
    view.root.traverse((o) => {
      if (o instanceof Mesh && o.renderOrder === RENDER_ORDER.player) parts.push(o);
    });
    expect(parts).toHaveLength(5);
    for (const part of parts) {
      const [ghost, ...rest] = part.children.filter((c): c is Mesh => c instanceof Mesh);
      if (!ghost) throw new Error('pieza sin silueta');
      expect(rest).toHaveLength(0);
      expect(ghost.geometry).toBe(part.geometry);
      expect(ghost.renderOrder).toBe(RENDER_ORDER.playerSilhouette);
      expect(ghost.material).toBeInstanceOf(MeshBasicMaterial);
      const material = ghost.material as MeshBasicMaterial;
      expect(material.depthFunc).toBe(GreaterDepth);
      expect(material.depthWrite).toBe(false);
    }
  });

  it('la silueta se dibuja tras lo que puede taparla y antes que la abuela; la hierba, después', () => {
    expect(RENDER_ORDER.default).toBeLessThan(RENDER_ORDER.playerSilhouette);
    expect(RENDER_ORDER.playerSilhouette).toBeLessThan(RENDER_ORDER.player);
    expect(RENDER_ORDER.player).toBeLessThan(RENDER_ORDER.afterPlayer);
  });
});
