# Build stage
FROM node:20-alpine AS builder

WORKDIR /app

# Build arguments for environment variables (baked into the static build)
ARG API_BASE_URL="https://alexandria-api.delightfulocean-4bd7ffeb.eastus2.azurecontainerapps.io/api"
ARG GOOGLE_WEB_CLIENT_ID=""

# Set environment variables for the build
ENV API_BASE_URL=$API_BASE_URL
ENV GOOGLE_WEB_CLIENT_ID=$GOOGLE_WEB_CLIENT_ID

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY . .

# Build the web version
RUN npx expo export --platform web

# Production stage - serve with nginx
FROM nginx:alpine

# Copy the built web app to nginx
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy custom nginx config
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
