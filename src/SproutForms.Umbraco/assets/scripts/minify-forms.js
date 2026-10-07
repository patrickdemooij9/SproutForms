#!/usr/bin/env node

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { minify as minifyJs } from 'terser';
import cssnano from 'cssnano';
import postcss from 'postcss';
import { build } from 'vite';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const srcDir = path.join(__dirname, '../../../SproutForms.Core/wwwroot/forms-src');
const outDir = path.join(__dirname, '../../../SproutForms.Core/wwwroot');

const filesConfig = [
    { src: 'forms.ts', out: 'forms.js', type: 'ts' },
    { src: 'forms-layout.css', type: 'css' },
    { src: 'forms-default-theme.css', type: 'css' }
];

// forms.ts imports the conditions and validation of @sproutforms/client (src/SproutForms.Client), so it is bundled
// into one script, not compiled on its own
async function bundleTypeScript(srcPath, outPath) {
    try {
        await build({
            configFile: false,
            logLevel: 'warn',
            build: {
                lib: {
                    entry: srcPath,
                    formats: ['iife'],
                    name: 'SproutFormsBundle',
                    fileName: () => path.basename(outPath)
                },
                outDir: path.dirname(outPath),
                emptyOutDir: false,
                minify: 'terser',
                sourcemap: false,
                target: 'es2020'
            }
        });
        return true;
    } catch (error) {
        console.error('✗ TypeScript bundling error:', error.message);
        return false;
    }
}

async function minifyFile(config) {
    const srcPath = path.join(srcDir, config.src);
    const outName = config.out || config.src;
    const outPath = path.join(outDir, outName);

    if (!fs.existsSync(srcPath)) {
        console.warn(`⚠️  Source file not found: ${srcPath}`);
        return;
    }

    try {
        const content = fs.readFileSync(srcPath, 'utf-8');

        if (config.type === 'ts') {
            const success = await bundleTypeScript(srcPath, outPath);
            if (!success) {
                console.error(`✗ Failed to bundle TypeScript: ${config.src}`);
                process.exit(1);
            }
            console.log(`✓ Bundled and minified: ${config.src} -> ${outName}`);
        } else if (config.type === 'js') {
            const result = await minifyJs(content);
            fs.writeFileSync(outPath, result.code);
            console.log(`✓ Minified: ${config.src}`);
        } else if (config.type === 'css') {
            const { css } = await postcss([cssnano()]).process(content, { from: srcPath, to: outPath });
            fs.writeFileSync(outPath, css);
            console.log(`✓ Minified: ${config.src}`);
        }
    } catch (error) {
        console.error(`✗ Error processing ${config.src}:`, error.message);
        process.exit(1);
    }
}

async function minifyAll() {
    console.log('🔨 Minifying forms...');
    
    if (!fs.existsSync(srcDir)) {
        console.log('ℹ️  Creating forms-src directory...');
        fs.mkdirSync(srcDir, { recursive: true });
    }

    for (const config of filesConfig) {
        await minifyFile(config);
    }

    console.log('✅ Done!');
}

function watchFiles() {
    console.log('👀 Watching forms-src for changes...');
    
    fs.watch(srcDir, { recursive: true }, async (eventType, filename) => {
        if (filename) {
            const config = filesConfig.find(f => f.src === filename);
            if (config) {
                console.log(`\n📝 ${filename} changed, processing...`);
                await minifyFile(config);
            }
        }
    });
}

const isWatch = process.argv.includes('--watch');

if (isWatch) {
    minifyAll().then(() => watchFiles());
} else {
    minifyAll();
}
