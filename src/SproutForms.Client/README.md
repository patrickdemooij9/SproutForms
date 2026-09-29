# @sproutforms/client

A framework-agnostic client for the [SproutForms](https://github.com/patrickdemooij9/SproutForms) headless API. It fetches a published form, evaluates its conditions and validation rules the way the server does, and submits it. It doesn't render anything.

Turn the headless API on in the Umbraco site first. The [README's Headless section](https://github.com/patrickdemooij9/SproutForms#headless) covers the settings, the endpoints and the client.

```ts
import { createSproutFormsClient, validateForm } from '@sproutforms/client';

const client = createSproutFormsClient({ baseUrl: 'https://cms.example.com' });
const form = await client.getDefinition('contact');
const errors = await validateForm(form, values);
const result = await client.submit(form, { values });
```

## Development

- `npm run build` compiles to `dist`.
- `npm run generate` regenerates `src/api/types.gen.ts` from the OpenAPI document. Run it while the demo site's `AiTest` profile is running.
- `example/` is a playground that renders any form of the demo site and shows every request and the stored submission. `pwsh -File scripts/ai-test/run-headless-playground.ps1` from the repository root starts it with the demo site.

The condition and validation code is also bundled into the package's `forms.js` by `npm run minify:forms` in `src/SproutForms.Umbraco/assets`. Run that after changing `conditions.ts` or `validation.ts`.
