# ScannerZero Web

Simple Angular SSR promotional and support site for ScannerZero.

## Development

```sh
npm install
npm run dev
```

## Production Build

```sh
npm run build
npm run start
```

Open `http://localhost:4000`.

Angular SSR validates hostnames. Localhost is enabled by default; for a deployed host set `NG_ALLOWED_HOSTS` to a comma-separated list or `*` in the container environment.

## Docker

```sh
docker build -t scannerzero-web .
docker run --rm -p 4000:4000 scannerzero-web
```

The Docker image serves the SSR build on port `4000`.

The scanner strategy and payload type demo pages are served from `public/demos/`.

Screenshots can be added under `public/assets/screenshots/` and wired into the screenshots section in `src/app/app.ts`.
