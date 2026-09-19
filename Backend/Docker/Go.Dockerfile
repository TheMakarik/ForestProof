# syntax=docker/dockerfile:1

FROM golang:1.26-alpine AS build
WORKDIR /src

COPY Backend/Go/go.mod ./
RUN go mod download

COPY Backend/Go/ ./
RUN CGO_ENABLED=0 GOOS=linux go build -trimpath -ldflags="-s -w" -o /out/gateway ./cmd/gateway

FROM alpine:3.21 AS runtime
RUN apk add --no-cache ca-certificates

WORKDIR /app
COPY --from=build /out/gateway /app/gateway
COPY Backend/Dotnet/ForestProof.Backend/Dataset /data

ENV GATEWAY_ADDR=:5300 \
    CSHARP_BASE_URL=http://backend:8080 \
    DATA_ROOT=/data \
    GATEWAY_CACHE_DIR=/cache

RUN mkdir -p /cache

EXPOSE 5300
ENTRYPOINT ["/app/gateway"]
