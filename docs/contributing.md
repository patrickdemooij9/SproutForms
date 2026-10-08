# Contributing

How to build SproutForms, run its demo sites and try out the headless packages. [`src/AGENTS.md`](../src/AGENTS.md) describes the architecture: the projects, the models, the services and how the Umbraco and standalone hosts differ.

## The projects

| Project | |
|---|---|
| `src/SproutForms.Core` | Everything that doesn't need Umbraco: forms in code, rendering (views, forms.js, CSS), submissions, workflows, in-memory storage. The `SproutForms.Core` NuGet package. |
| `src/SproutForms.Umbraco.Core` | The Umbraco plugin: backoffice API, repositories, descriptors, the headless API. |
| `src/SproutForms.Umbraco` | The Umbraco package, with the backoffice front-end in `assets`. The `SproutForms.Umbraco` NuGet package. |
| `src/SproutForms.Site` | The Umbraco demo site. |
| `src/SproutForms.Standalone.Site` | An ASP.NET Core MVC site without Umbraco, using `SproutForms.Core` alone. |
| `src/SproutForms.Client` | `@sproutforms/client`, with an example app in `example/`. |
| `src/SproutForms.Vue` | `@sproutforms/vue`, with a playground in `playground/`. |
| `src/SproutForms.Nuxt` | `@sproutforms/nuxt`, with a playground in `playground/`. |
| `src/SproutForms.Core.Tests` | Tests for `SproutForms.Core` (NUnit). |

## Building

The backend targets .NET 10:

```
dotnet build src/SproutForms.sln
dotnet test src/SproutForms.Core.Tests
```

The backoffice front-end, in `src/SproutForms.Umbraco/assets`:

```
npm install
npm run build
```

That also bundles forms.js (`npm run minify:forms`), from `src/SproutForms.Core/wwwroot/forms-src/forms.ts` and the code it uses from `@sproutforms/client`. Run `npm run minify:forms` after changing either.

The headless packages are an npm workspace with its root in the repository root:

```
npm install
npm run build       # builds @sproutforms/client, @sproutforms/vue and @sproutforms/nuxt, in that order
npm run typecheck
```

The npm packages aren't published yet. To use them in another project before they are, build them and install them from their folders (such as `npm install ../SproutForms/src/SproutForms.Vue`), or with `npm pack`.

## The demo sites

`src/SproutForms.Site` is an Umbraco site with SproutForms. Its examples are in `Examples`: a quiz, a poll and a product finder built on [form types](extending/form-types.md), and forms that use [calculations](forms/calculations.md). Its `Views/Forms/Themes/Example` is an example [theme](styling.md#themes-your-own-markup).

`scripts/ai-test/run-site.ps1` runs it in the `AiTest` environment on `http://localhost:5970`: a throwaway SQLite database, an unattended install, emails written to `umbraco/Data/AiTest/Mail`, and the headless API on, with the playgrounds' origins allowed. `-Reset` starts from a clean database, and `-AdminPassword` sets the admin's password for a backoffice session.

`src/SproutForms.Standalone.Site` runs without Umbraco on `http://localhost:5180` (`dotnet run --project src/SproutForms.Standalone.Site`). Its emails go to `App_Data/Emails`.

## The headless playground

[`src/SproutForms.Client/example`](../src/SproutForms.Client/example) is a playground for the headless API. It renders any form of the demo site with plain TypeScript, the way a front-end would (`src/form-renderer.ts`), and shows next to it:

- **Requests**: every call to the API, with its headers and bodies.
- **Definition**: what the definition endpoint returned.
- **Stored submission**: the form's newest submission and its workflows.
- **Raw request**: send any body to the API, such as a value the form can't produce.

Switches in its header skip the validation in the browser, so you see the server's, or fill in the honeypot. It can also send an API key.

Start the demo site and the playground together, which opens `http://localhost:5173`:

```
pwsh -File scripts/ai-test/run-headless-playground.ps1
```

`-Reset` starts from a clean database.

## Vue and Nuxt

With the demo site running (`pwsh -File scripts/ai-test/run-site.ps1`), from the repository root:

- `npm run dev:vue` starts the Vue playground on `http://localhost:5174`. It renders the `aiTestHeadlessBasics` form by default (`?form=` for another one), with an alias override and an `inline` theme to switch to.
- `npm run dev:nuxt` starts the Nuxt playground on `http://localhost:3000`. It renders the `aiTestHeadlessBasics` form on the server (`?form=` for another one), with a field type override in `playground/plugins/sproutforms.ts` and an alias override. Build `@sproutforms/client` and `@sproutforms/vue` first.
- `npm run preview:nuxt` builds the Nuxt playground and serves the production build.

The Nuxt playground runs on Nuxt 4.5.2: with Nuxt 4.6.0, a plain app in this workspace fails every server render with "Either manifest or precomputed data must be provided", also without this module.

After changing the headless API's models, regenerate `src/SproutForms.Client/src/api/types.gen.ts` with `npm run generate` in `src/SproutForms.Client` while the demo site's `AiTest` profile runs.
