# Stage 1: Build React app
FROM node:22 AS builder
WORKDIR /app

# Accept build arguments for version information
ARG VITE_COMMIT_SHA=unknown
ARG VITE_DOTNET_VERSION=9.0
ARG VITE_ASPIRE_VERSION=13.0.0

# Set them as environment variables for the build
ENV VITE_COMMIT_SHA=$VITE_COMMIT_SHA
ENV VITE_DOTNET_VERSION=$VITE_DOTNET_VERSION
ENV VITE_ASPIRE_VERSION=$VITE_ASPIRE_VERSION

COPY . .
RUN npm install
RUN npm run build

# Stage 2: Copy static files to YARP container  
FROM mcr.microsoft.com/dotnet/nightly/yarp:2.3.0-preview.4 AS yarp
WORKDIR /app
COPY --from=builder /app/dist ./wwwroot