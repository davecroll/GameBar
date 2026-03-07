import { defineConfig } from 'vite';

export default defineConfig({
  build: {
    lib: {
      entry: 'src/gameBarPixi.ts',
      formats: ['es'],
      fileName: 'gameBarPixi'
    },
    outDir: 'wwwroot/dist',
    emptyOutDir: false
  }
});

