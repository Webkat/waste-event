# ---- Build stage ----
FROM node:22 AS build
WORKDIR /app

# Installer afhaengigheder foerst (caches hvis package-filerne ikke aendrer sig).
COPY package.json package-lock.json ./
RUN npm install

# Kopier resten og byg appen til statiske filer i /app/dist.
COPY . .
RUN npm run build

# ---- Runtime stage ----
FROM nginx:alpine AS runtime

# Erstat nginx standard-config med vores egen (server app + proxy /api).
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Kopier de byggede statiske filer ind i nginx web-roden.
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
