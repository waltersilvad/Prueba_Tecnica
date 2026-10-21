# Frontend React. Contexto de compilación: carpeta frontend/.

# ---------- Compilación ----------
FROM node:24-alpine AS compilacion
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Vite incrusta la URL de la API al compilar: es la que usará el navegador del evaluador.
ARG VITE_API_URL=http://localhost:5080
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# ---------- Ejecución ----------
# nginx sin privilegios: corre como usuario no root y escucha en el puerto 8080.
FROM nginxinc/nginx-unprivileged:1.29-alpine AS ejecucion
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=compilacion /app/dist /usr/share/nginx/html
EXPOSE 8080
