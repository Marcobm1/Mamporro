import { defineConfig } from 'vitest/config';

export default defineConfig({
  // Rutas relativas: la build funciona servida desde cualquier carpeta.
  base: './',
  build: {
    target: 'es2022',
    sourcemap: true,
    chunkSizeWarningLimit: 1500,
  },
  server: {
    port: 5173,
  },
  test: {
    environment: 'node',
    include: ['src/**/*.test.ts'],
  },
});
