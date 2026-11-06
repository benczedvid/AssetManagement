FROM node:22-alpine AS build

WORKDIR /app

COPY package.json package-lock.json ./

RUN npm ci

COPY . .

RUN npm run build -- --configuration production


FROM nginx:alpine AS runtime

COPY nginx/default.conf /etc/nginx/conf.d/default.conf

COPY docker/40-generate-runtime-config.sh \
    /docker-entrypoint.d/40-generate-runtime-config.sh

RUN chmod +x \
    /docker-entrypoint.d/40-generate-runtime-config.sh

COPY --from=build \
    /app/dist/asset-management/browser \
    /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]