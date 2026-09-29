import { defineConfig } from '@hey-api/openapi-ts';

// Only the types: the client in src/client.ts does its own fetching, since the submit body is JSON or multipart
export default defineConfig({
    input: 'http://localhost:5970/umbraco/swagger/sproutforms-delivery/swagger.json',
    output: {
        path: 'src/api',
    },
    plugins: [
        {
            name: '@hey-api/typescript',
            enums: 'javascript',
        },
    ],
});
