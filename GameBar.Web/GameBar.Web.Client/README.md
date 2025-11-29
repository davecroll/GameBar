# GameBar.Web.Client — ES Module (Vite + TypeScript + PixiJS)

What was added
- TypeScript + Vite toolchain under this project
- PixiJS wrapper module exporting init/render/destroy at `src/gameBarPixi.ts`
- Vite builds an ES module to `wwwroot/dist/gameBarPixi.js`
- `GameClientService` dynamically imports the module via `IJSRuntime` and calls its methods
- MSBuild target runs `npm ci && npm run build` during publish so assets are present

Dev quickstart (PowerShell)
```
cd C:\Users\dave\dev\GameBar\GameBar.Web\GameBar.Web.Client
npm install
npm run build
```
Then run your Blazor app normally (server or WASM host). The app will import `/dist/gameBarPixi.js` at runtime.

Notes
- If the app is hosted under a sub-path, the import path is constructed via `NavigationManager.BaseUri` to stay correct.
- For faster iteration, you can run `npm run dev` (Vite dev server) and temporarily point the C# import to the dev URL, but the default flow builds to `wwwroot/dist`.
- To avoid stale caches in production, consider enabling hashed filenames in Vite later and versioning the import path.

