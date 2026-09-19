# syntax=docker/dockerfile:1

FROM node:22-alpine AS build
WORKDIR /app

COPY Frontend/package.json Frontend/package-lock.json ./
RUN npm ci

COPY Frontend/ ./

ARG VITE_USE_MOCK=false
ARG VITE_API_BASE=/api/v1
ENV VITE_USE_MOCK=${VITE_USE_MOCK} \
    VITE_API_BASE=${VITE_API_BASE}

RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY Backend/Docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
