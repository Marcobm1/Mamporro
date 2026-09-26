// Renderizado retro: la escena se dibuja en un render target de baja resolución y
// luego se escala a pantalla con un factor ENTERO (todos los píxeles miden igual),
// con filtrado Nearest y, opcionalmente, reducción a color de 15 bits con tramado.
import {
  HalfFloatType,
  Mesh,
  NearestFilter,
  OrthographicCamera,
  PlaneGeometry,
  Scene,
  ShaderMaterial,
  Vector2,
  WebGLRenderer,
  WebGLRenderTarget,
  type Camera,
} from 'three';
import { retroUniforms } from './retroMaterial';

const POST_VERTEX = /* glsl */ `
void main() {
  gl_Position = vec4(position.xy, 0.0, 1.0);
}
`;

const POST_FRAGMENT = /* glsl */ `
uniform sampler2D tScene;
uniform vec2 uInternalSize;
uniform vec2 uOffset;
uniform float uScale;
uniform float uDither;

// Matriz de Bayer 4x4 (tramado ordenado) calculada sin tablas.
float bayer2(vec2 a) { a = floor(a); return fract(dot(a, vec2(0.5, a.y * 0.75))); }
float bayer4(vec2 a) { return bayer2(0.5 * a) * 0.25 + bayer2(a); }

void main() {
  vec2 px = floor((gl_FragCoord.xy + uOffset) / uScale);
  vec2 uv = (px + 0.5) / uInternalSize;
  vec4 color = linearToOutputTexel(texture2D(tScene, uv));
  if (uDither > 0.5) {
    // 5 bits por canal (como la PS1) con tramado a nivel de píxel interno.
    float threshold = bayer4(px) - 0.5;
    color.rgb = floor(color.rgb * 31.0 + 0.5 + threshold) / 31.0;
  }
  gl_FragColor = vec4(clamp(color.rgb, 0.0, 1.0), 1.0);
}
`;

export class RetroRenderer {
  readonly gl: WebGLRenderer;
  private readonly target: WebGLRenderTarget;
  private readonly postScene = new Scene();
  private readonly postCamera = new OrthographicCamera(-1, 1, 1, -1, 0, 1);
  private readonly postMaterial: ShaderMaterial;

  private targetHeight = 360;
  private bufferWidth = 0;
  private bufferHeight = 0;
  internalWidth = 1;
  internalHeight = 1;
  pixelScale = 1;

  constructor(private readonly canvas: HTMLCanvasElement) {
    this.gl = new WebGLRenderer({
      canvas,
      antialias: false,
      alpha: false,
      stencil: false,
      powerPreference: 'high-performance',
    });
    this.gl.setPixelRatio(1);
    // Con dos pasadas por frame, reiniciamos las estadísticas a mano.
    this.gl.info.autoReset = false;

    this.target = new WebGLRenderTarget(1, 1, {
      type: HalfFloatType,
      minFilter: NearestFilter,
      magFilter: NearestFilter,
      generateMipmaps: false,
      depthBuffer: true,
    });

    this.postMaterial = new ShaderMaterial({
      uniforms: {
        tScene: { value: this.target.texture },
        uInternalSize: { value: new Vector2(1, 1) },
        uOffset: { value: new Vector2(0, 0) },
        uScale: { value: 1 },
        uDither: { value: 1 },
      },
      vertexShader: POST_VERTEX,
      fragmentShader: POST_FRAGMENT,
      depthTest: false,
      depthWrite: false,
    });
    const quad = new Mesh(new PlaneGeometry(2, 2), this.postMaterial);
    quad.frustumCulled = false;
    this.postScene.add(quad);

    this.resize(true);
  }

  get aspect(): number {
    return this.internalWidth / this.internalHeight;
  }

  setTargetHeight(height: number): void {
    this.targetHeight = height;
    this.resize(true);
  }

  setDithering(enabled: boolean): void {
    (this.postMaterial.uniforms.uDither as { value: number }).value = enabled ? 1 : 0;
  }

  setVertexSnap(enabled: boolean): void {
    retroUniforms.uSnapEnabled.value = enabled ? 1 : 0;
  }

  /** Ajusta los tamaños al del canvas. Devuelve `true` si algo cambió. */
  resize(force = false): boolean {
    const dpr = window.devicePixelRatio || 1;
    const width = Math.max(1, Math.floor(this.canvas.clientWidth * dpr));
    const height = Math.max(1, Math.floor(this.canvas.clientHeight * dpr));
    if (!force && width === this.bufferWidth && height === this.bufferHeight) return false;

    this.bufferWidth = width;
    this.bufferHeight = height;
    this.gl.setSize(width, height, false);

    // Factor entero más cercano a la altura objetivo; la imagen interna cubre
    // la pantalla completa y se recorta como mucho (escala - 1) píxeles por lado.
    const scale = Math.max(1, Math.round(height / this.targetHeight));
    this.pixelScale = scale;
    this.internalWidth = Math.ceil(width / scale);
    this.internalHeight = Math.ceil(height / scale);
    this.target.setSize(this.internalWidth, this.internalHeight);

    const u = this.postMaterial.uniforms;
    (u.uInternalSize as { value: Vector2 }).value.set(this.internalWidth, this.internalHeight);
    (u.uOffset as { value: Vector2 }).value.set(
      (this.internalWidth * scale - width) / 2,
      (this.internalHeight * scale - height) / 2,
    );
    (u.uScale as { value: number }).value = scale;
    retroUniforms.uSnapResolution.value.set(this.internalWidth / 2, this.internalHeight / 2);
    return true;
  }

  render(scene: Scene, camera: Camera): void {
    this.gl.info.reset();
    this.gl.setRenderTarget(this.target);
    this.gl.render(scene, camera);
    this.gl.setRenderTarget(null);
    this.gl.render(this.postScene, this.postCamera);
  }

  dispose(): void {
    this.target.dispose();
    this.postMaterial.dispose();
    this.gl.dispose();
  }
}
