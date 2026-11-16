import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  base: process.env.BASE_PATH || '/',
  publicDir: '../data',
  test: { environment: 'jsdom', setupFiles: ['./src/test-setup.ts'], globals: true },
});
