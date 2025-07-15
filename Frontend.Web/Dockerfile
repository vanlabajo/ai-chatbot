# Base image
FROM node:24 AS base
WORKDIR /app

# Build stage
FROM base AS builder

# Create the env file (these should come from --build-arg or ARGs)
ARG NEXT_PUBLIC_CLIENT_ID
ARG NEXT_PUBLIC_TENANT_ID
ARG NEXT_PUBLIC_REDIRECT_URI
ARG NEXT_PUBLIC_SCOPE
ARG NEXT_PUBLIC_API_ENDPOINT
ARG NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING

ENV NODE_OPTIONS=--max-old-space-size=4096

RUN echo "NEXT_PUBLIC_CLIENT_ID=$NEXT_PUBLIC_CLIENT_ID" >> .env.production && \
  echo "NEXT_PUBLIC_TENANT_ID=$NEXT_PUBLIC_TENANT_ID" >> .env.production && \
  echo "NEXT_PUBLIC_REDIRECT_URI=$NEXT_PUBLIC_REDIRECT_URI" >> .env.production && \
  echo "NEXT_PUBLIC_SCOPE=$NEXT_PUBLIC_SCOPE" >> .env.production && \
  echo "NEXT_PUBLIC_API_ENDPOINT=$NEXT_PUBLIC_API_ENDPOINT" >> .env.production && \
  echo "NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING=$NEXT_PUBLIC_APPINSIGHTS_CONNECTION_STRING" >> .env.production

COPY package.json pnpm-lock.yaml* ./
RUN npm install -g pnpm && pnpm install

COPY . .
RUN pnpm build

# Production stage
FROM base AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NODE_OPTIONS=--max-old-space-size=4096

# Install only production dependencies
COPY package.json pnpm-lock.yaml* ./
RUN npm install -g pnpm && pnpm install --prod

# Copy built output and public assets
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public

EXPOSE 3000

CMD ["pnpm", "start"]