// Parches de shader para los materiales estándar de Three:
// - "vertex snapping" estilo PS1 (siempre): los vértices se ajustan a la rejilla
//   de píxeles de la imagen interna, con el característico temblor;
// - viento (opcional): balanceo de hierba, flores y copas de árboles;
// - destello (opcional): atributo por instancia que blanquea el color (golpes);
// - silueta (opcional): solo se dibuja donde algo tapa al objeto, con tramado.
import {
  GreaterDepth,
  MeshBasicMaterial,
  Vector2,
  Vector3,
  type Material,
  type WebGLProgramParametersWithUniforms,
} from 'three';

/** Uniforms compartidos por todos los materiales parcheados (se cambian en caliente). */
export const retroUniforms = {
  uSnapEnabled: { value: 1 },
  /** Mitad de la resolución interna: en NDC (-1..1) cada píxel mide 1/uSnapResolution. */
  uSnapResolution: { value: new Vector2(320, 180) },
  /** Tiempo en segundos (para el viento). */
  uTime: { value: 0 },
};

export interface WindOptions {
  /** Desplazamiento por metro de altura sobre `start`. */
  amplitude: number;
  /** Altura local desde la que empieza a moverse (la base queda fija). */
  start: number;
  speed: number;
}

export interface FlapOptions {
  /** Cuánto suben las puntas de las alas por metro de distancia al centro (eje X local). */
  amplitude: number;
  speed: number;
}

export interface RetroFeatures {
  wind?: WindOptions;
  /** Aleteo: las alas (vértices con |x| grande) suben y bajan. */
  flap?: FlapOptions;
  /** Añade el atributo por instancia `aFlash` (0..1) que mezcla el color con blanco. */
  flash?: boolean;
  /** Pasada de silueta: ver `createSilhouetteMaterial`. */
  silhouette?: boolean;
}

/** Nombre del atributo de destello en las geometrías instanciadas. */
export const FLASH_ATTRIBUTE = 'aFlash';

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

/**
 * Silueta: acerca cada vértice a la cámara a lo largo de su propio rayo de vista
 * (en pantalla queda en el mismo sitio). Así solo aparece tras obstáculos que
 * están al menos a SILHOUETTE_BIAS metros por delante: el suelo bajo los pies o
 * un bulto pequeño pegado al cuerpo no la hacen asomar.
 */
const SILHOUETTE_VERTEX = /* glsl */ `
mvPosition.xyz *= max(0.0, 1.0 - SILHOUETTE_BIAS / max(length(mvPosition.xyz), 0.001));
gl_Position = projectionMatrix * mvPosition;
`;

/** Tramado de tablero de ajedrez (un píxel de la imagen interna sí, otro no). */
const SILHOUETTE_FRAGMENT = /* glsl */ `
if (mod(floor(gl_FragCoord.x) + floor(gl_FragCoord.y), 2.0) < 0.5) discard;
`;

const WIND_UNIFORMS = /* glsl */ `
uniform vec3 uWind;
`;

const WIND_CODE = /* glsl */ `
{
  #ifdef USE_INSTANCING
    vec2 windBase = vec2(instanceMatrix[3].x, instanceMatrix[3].z);
  #else
    vec2 windBase = vec2(modelMatrix[3].x, modelMatrix[3].z);
  #endif
  float windH = max(0.0, transformed.y - uWind.y);
  float windPhase = uTime * uWind.z + dot(windBase, vec2(0.13, 0.17));
  transformed.x += sin(windPhase) * windH * uWind.x;
  transformed.z += cos(windPhase * 0.83) * windH * uWind.x * 0.6;
}
`;

function patch(shader: WebGLProgramParametersWithUniforms, features: RetroFeatures): void {
  shader.uniforms.uSnapEnabled = retroUniforms.uSnapEnabled;
  shader.uniforms.uSnapResolution = retroUniforms.uSnapResolution;
  let header = SNAP_UNIFORMS;
  let beginVertex = '';
  let projectVertex = '';
  if (features.wind || features.flap) {
    shader.uniforms.uTime = retroUniforms.uTime;
    header += 'uniform float uTime;\n';
  }

  if (features.wind) {
    shader.uniforms.uWind = { value: new Vector3(features.wind.amplitude, features.wind.start, features.wind.speed) };
    header += WIND_UNIFORMS;
    beginVertex += WIND_CODE;
  }
  if (features.flap) {
    shader.uniforms.uFlap = { value: new Vector2(features.flap.amplitude, features.flap.speed) };
    header += 'uniform vec2 uFlap;\n';
    beginVertex += `{
      #ifdef USE_INSTANCING
        float flapPhase = dot(vec2(instanceMatrix[3].x, instanceMatrix[3].z), vec2(0.71, 0.37));
      #else
        float flapPhase = 0.0;
      #endif
      transformed.y += abs(transformed.x) * sin(uTime * uFlap.y + flapPhase) * uFlap.x;
    }
`;
  }
  if (features.flash) {
    header += `attribute float ${FLASH_ATTRIBUTE};\nvarying float vFlash;\n`;
    beginVertex += `vFlash = ${FLASH_ATTRIBUTE};\n`;
    shader.fragmentShader = shader.fragmentShader
      .replace('#include <common>', '#include <common>\nvarying float vFlash;')
      .replace('#include <dithering_fragment>', '#include <dithering_fragment>\ngl_FragColor.rgb = mix(gl_FragColor.rgb, vec3(1.0), vFlash);');
  }

  if (features.silhouette) {
    header += '#define SILHOUETTE_BIAS 0.35\n';
    projectVertex += SILHOUETTE_VERTEX;
    shader.fragmentShader = shader.fragmentShader.replace(
      '#include <clipping_planes_fragment>',
      `#include <clipping_planes_fragment>\n${SILHOUETTE_FRAGMENT}`,
    );
  }

  shader.vertexShader = shader.vertexShader
    .replace('#include <common>', `#include <common>\n${header}`)
    .replace('#include <begin_vertex>', `#include <begin_vertex>\n${beginVertex}`)
    // El ajuste a la rejilla de píxeles va el último, sobre la posición final.
    .replace('#include <project_vertex>', `#include <project_vertex>\n${projectVertex}${SNAP_CODE}`);
}

/** Aplica el efecto PS1 (y los extra pedidos) a un material estándar de Three. */
export function applyRetro<T extends Material>(material: T, features: RetroFeatures = {}): T {
  const key = `retro${features.wind ? '+wind' : ''}${features.flap ? '+flap' : ''}${features.flash ? '+flash' : ''}${features.silhouette ? '+silhouette' : ''}`;
  material.onBeforeCompile = (shader) => patch(shader, features);
  // Todas las variantes comparten la función de arriba: la clave distingue sus shaders.
  material.customProgramCacheKey = () => key;
  return material;
}

/**
 * Material de silueta: color plano con tramado que solo se dibuja donde el objeto
 * queda detrás de algo ya dibujado (prueba de profundidad invertida). Hay que
 * dibujarlo antes que el objeto normal (ver RENDER_ORDER): lo que se ve del
 * objeto tapa su propia silueta y lo tapado se queda con ella.
 */
export function createSilhouetteMaterial(color: number): MeshBasicMaterial {
  const material = new MeshBasicMaterial({ color, fog: false, depthWrite: false, depthFunc: GreaterDepth });
  return applyRetro(material, { silhouette: true });
}
