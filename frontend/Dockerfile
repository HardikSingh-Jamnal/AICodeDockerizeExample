# build environment
FROM node:18-alpine as build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# production environment
FROM node:18-alpine
WORKDIR /app

# Copy the standalone output and required files
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public

# Ensure the server listens on all interfaces (0.0.0.0) not just localhost
ENV HOSTNAME "0.0.0.0"
ENV PORT 3000

EXPOSE 3000

CMD ["node", "server.js"]
