FROM node:24-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM node:24-alpine AS runtime
ENV NODE_ENV=production
ENV PORT=4000
ENV NG_ALLOWED_HOSTS=*
WORKDIR /app
COPY --from=build /app/dist/scannerzero-web ./dist/scannerzero-web
COPY --from=build /app/server.mjs ./server.mjs
EXPOSE 4000
CMD ["node", "server.mjs"]
