# Monolegal Billing frontend

Phase 1 provides a static landing page using Angular 22.2.3 standalone components
with strict TypeScript and template checking. Invoice views and reminder actions
belong to later phases.

## Run locally

From `frontend/monolegal-billing`, using Node.js 24.19.0 and npm 11.17.0:

```sh
npm ci
npm start -- --host localhost
```

Open `http://localhost:4200`. This static page runs independently of the backend.

## Verify

```sh
npm run build
npm test -- --watch=false
```

The production build is written to `dist/monolegal-billing/browser`.
Vitest runs the component rendering test with jsdom; it does not require a browser.
Angular packages and CLI are pinned to 22.2.3, and `package-lock.json` locks the
resolved dependency tree for `npm ci`.
