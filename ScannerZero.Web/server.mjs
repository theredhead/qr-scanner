import { createServer } from 'node:http';
import { reqHandler } from './dist/scannerzero-web/server/server.mjs';

const port = process.env.PORT ?? 4000;
const host = process.env.HOST ?? '0.0.0.0';
const server = createServer(reqHandler);

server.listen(Number(port), host, () => {
  console.log(`ScannerZero web server listening on http://${host}:${port}`);
});
