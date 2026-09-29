import { defineConfig } from 'vite';
import { fileURLToPath } from 'url';

export default defineConfig({
    resolve: {
        // The client's source, so the example runs without building the package first
        alias: {
            '@sproutforms/client': fileURLToPath(new URL('../src/index.ts', import.meta.url))
        }
    },
    server: {
        // One of SproutForms:Headless:AllowedOrigins in the demo site's appsettings.AiTest.json
        port: 5173,
        strictPort: true,
        // The demo site's /ai-test endpoints (form list, stored submissions) have no CORS, so they go through Vite.
        // The headless API itself is called cross-origin, as a real front-end would
        proxy: {
            '/ai-test': process.env.SPROUTFORMS_URL ?? 'http://localhost:5970'
        }
    }
});
