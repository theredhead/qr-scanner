import { CommonEngine, createNodeRequestHandler, isMainModule } from '@angular/ssr/node';
import express from 'express';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import bootstrap from './main.server';

const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');
const indexHtml = join(serverDistFolder, 'index.server.html');

const app = express();
const allowedHosts = (process.env['NG_ALLOWED_HOSTS'] ?? 'localhost,127.0.0.1')
  .split(',')
  .map((host) => host.trim())
  .filter(Boolean);
const commonEngine = new CommonEngine({ allowedHosts });
let runningServer: ReturnType<typeof app.listen> | undefined;

app.use(express.static(browserDistFolder, {
  maxAge: '1y',
  index: false,
  redirect: false
}));

app.get('/favicon.ico', (_req, res) => {
  res.redirect(302, '/assets/app-icon.png');
});

app.use((req, res, next) => {
  const protocol = req.protocol;
  const host = req.get('host') ?? 'localhost';
  const url = `${protocol}://${host}${req.originalUrl}`;

  commonEngine
    .render({
      bootstrap,
      documentFilePath: indexHtml,
      publicPath: browserDistFolder,
      url
    })
    .then((html) => res.send(html))
    .catch(next);
});

if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] ?? 4000;
  const host = process.env['HOST'] ?? '0.0.0.0';

  runningServer = app.listen(Number(port), host, () => {
    console.log(`ScannerZero web server listening on http://${host}:${port}`);
  });
}

export const reqHandler = createNodeRequestHandler(app);
