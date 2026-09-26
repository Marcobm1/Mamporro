// Parche de materiales estilo PS1: "vertex snapping" (los vértices se ajustan a la
// rejilla de píxeles de la imagen interna, produciendo el característico temblor).
import { Vector2, type Material, type WebGLProgramParametersWithUniforms } from 'three';

/** Uniforms compartidos por todos los materiales parcheados (se cambian en caliente). */
export const retroUniforms = {
  uSnapEnabled: { value: 1 },
  /** Mitad de la resolución interna: en NDC (-1..1) cada píxel mide 1/uSnapResolution. */
  uSnapResolution: { value: new Vector2(320, 180) },
};

const SNAP_UNIFORMS = /* glsl */ `
uniform float uSnapEnabled;
uniform vec2 uSnapResolution;
`;

const SNAP_CODE = /* glsl */ `
if (uSnapEnabled > 0.5 && gl_Position.w > 0.0) {
  vec2 ndc = gl_Position.xy / gl_Position.w;
  ndc = floor(ndc * uSnapResolution + 0.5) / uSnapResolution;
  gl_Position.xy = ndc * gl_Position.w;
}
`;

// Una sola función compartida: Three usa su código como parte de la clave de caché
// del programa, así todos los materiales iguales reutilizan el mismo shader.
function patchRetroShader(shader: WebGLProgramParametersWithUniforms): void {
  shader.uniforms.uSnapEnabled = retroUniforms.uSnapEnabled;
  shader.uniforms.uSnapResolution = retroUniforms.uSnapResolution;
  shader.vertexShader = shader.vertexShader
    .replace('#include <common>', `#include <common>\n${SNAP_UNIFORMS}`)
    .replace('#include <project_vertex>', `#include <project_vertex>\n${SNAP_CODE}`);
}

/** Aplica el efecto PS1 a un material estándar de Three (Lambert, Basic...). */
export function applyRetro<T extends Material>(material: T): T {
  material.onBeforeCompile = patchRetroShader;
  return material;
}
