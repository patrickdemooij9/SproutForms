import { fileURLToPath } from 'node:url';
import vue from '@vitejs/plugin-vue';
import { defineConfig } from 'vite';

export default defineConfig({
    root: fileURLToPath(new URL('.', import.meta.url)),
    plugins: [vue()],
    resolve: {
        // The packages' source, so the playground runs without building them first
        alias: {
            '@sproutforms/client': fileURLToPath(new URL('../../SproutForms.Client/src/index.ts', import.meta.url)),
            '@sproutforms/vue': fileURLToPath(new URL('../src/index.ts', import.meta.url))
        }
    },
    server: {
        // One of SproutForms:Headless:AllowedOrigins in the demo site's appsettings.AiTest.json
        port: 5174,
        strictPort: true
    }
});
