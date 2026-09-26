import type { WebGLProgramParametersWithUniforms, WebGLRenderer } from 'three';
import { describe, expect, it } from 'vitest';
import { createSilhouetteMaterial } from './retroMaterial';

/** Shader mínimo con los puntos de enganche que usan los parches. */
function fakeShader(): WebGLProgramParametersWithUniforms {
  return {
    uniforms: {},
    vertexShader: '#include <common>\nvoid main() {\n#include <begin_vertex>\n#include <project_vertex>\n}',
    fragmentShader: '#include <common>\nvoid main() {\n#include <clipping_planes_fragment>\n#include <dithering_fragment>\n}',
  } as unknown as WebGLProgramParametersWithUniforms;
}

describe('parches retro', () => {
  it('la silueta se acerca a la cámara antes de ajustarse a la rejilla de píxeles y usa tramado', () => {
    const material = createSilhouetteMaterial(0xffffff);
    const shader = fakeShader();
    material.onBeforeCompile(shader, {} as unknown as WebGLRenderer);
    const vs = shader.vertexShader;
    const project = vs.indexOf('#include <project_vertex>');
    const bias = vs.indexOf('SILHOUETTE_BIAS /');
    const snap = vs.indexOf('uSnapResolution + 0.5');
    expect(vs).toContain('#define SILHOUETTE_BIAS');
    expect(bias).toBeGreaterThan(project);
    expect(snap).toBeGreaterThan(bias);
    expect(shader.fragmentShader).toContain('discard');
    expect(material.customProgramCacheKey()).toContain('silhouette');
  });
});
