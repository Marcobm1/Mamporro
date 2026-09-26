// Cúpula de cielo con degradado y un sol pixelado. Sigue a la cámara y se dibuja
// siempre detrás de todo (profundidad en el plano lejano).
import { BackSide, Color, Mesh, ShaderMaterial, SphereGeometry, Vector3 } from 'three';
import { PALETTE } from './palette';
import { RENDER_ORDER } from './renderOrder';

const SKY_VERTEX = /* glsl */ `
varying vec3 vDir;
void main() {
  vDir = normalize(position);
  vec4 p = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
  gl_Position = p.xyww;
}
`;

const SKY_FRAGMENT = /* glsl */ `
uniform vec3 uZenith;
uniform vec3 uHorizon;
uniform vec3 uSunColor;
uniform vec3 uSunDir;
varying vec3 vDir;
void main() {
  vec3 dir = normalize(vDir);
  float h = clamp(dir.y, 0.0, 1.0);
  vec3 col = mix(uHorizon, uZenith, pow(h, 0.55));
  float s = dot(dir, uSunDir);
  col += uSunColor * pow(max(s, 0.0), 48.0) * 0.35;
  col = mix(col, uSunColor, step(0.9975, s));
  gl_FragColor = vec4(col, 1.0);
}
`;

/** Dirección hacia el sol (la luz direccional usa la misma). */
export const SUN_DIRECTION = new Vector3(0.45, 0.6, -0.66).normalize();

export function createSky(): Mesh {
  const material = new ShaderMaterial({
    uniforms: {
      uZenith: { value: new Color(PALETTE.skyZenith) },
      uHorizon: { value: new Color(PALETTE.skyHorizon) },
      uSunColor: { value: new Color(PALETTE.sun) },
      uSunDir: { value: SUN_DIRECTION.clone() },
    },
    vertexShader: SKY_VERTEX,
    fragmentShader: SKY_FRAGMENT,
    side: BackSide,
    depthWrite: false,
    fog: false,
  });
  const sky = new Mesh(new SphereGeometry(1, 24, 12), material);
  sky.frustumCulled = false;
  sky.renderOrder = RENDER_ORDER.sky;
  sky.scale.setScalar(100);
  return sky;
}
