import { copyFileSync, mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import vue from '@vitejs/plugin-vue';
import { defineConfig, type Plugin } from 'vite';

// The Razor forms' stylesheets stay the one source: the Vue components render the same classes and state attributes
const stylesheets = {
    'layout.css': '../SproutForms.Core/wwwroot/forms-src/forms-layout.css',
    'theme.css': '../SproutForms.Core/wwwroot/forms-src/forms-default-theme.css'
};

function copyStylesheets(): Plugin {
    return {
        name: 'sproutforms-copy-stylesheets',
        apply: 'build',
        closeBundle() {
            mkdirSync(fileURLToPath(new URL('dist', import.meta.url)), { recursive: true });
            for (const [target, source] of Object.entries(stylesheets)) {
                copyFileSync(fileURLToPath(new URL(source, import.meta.url)), fileURLToPath(new URL(`dist/${target}`, import.meta.url)));
            }
        }
    };
}

export default defineConfig({
    plugins: [vue(), copyStylesheets()],
    build: {
        lib: {
            entry: fileURLToPath(new URL('src/index.ts', import.meta.url)),
            formats: ['es'],
            fileName: 'index'
        },
        rollupOptions: {
            external: ['vue', '@sproutforms/client']
        },
        sourcemap: true
    }
});
