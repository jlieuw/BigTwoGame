# ── Build stage ───────────────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
RUN npm run build

# ── Serve with nginx ──────────────────────────────────────────────────────────
FROM nginx:alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html

# Store the config as a template; envsubst rewrites it at container start
# so BACKEND_URL can be injected without rebuilding the image.
COPY nginx.conf /etc/nginx/conf.d/default.conf.template

# envsubst only replaces ${BACKEND_URL}; nginx variables like $host are preserved.
CMD ["/bin/sh", "-c", \
     "envsubst '${BACKEND_URL}' < /etc/nginx/conf.d/default.conf.template \
      > /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"]

EXPOSE 80
