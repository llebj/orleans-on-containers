# syntax=docker/dockerfile:1

ARG PostgresVersion

FROM postgres:${PostgresVersion} AS base
WORKDIR /
COPY Scripts scripts
COPY pg-init.sh docker-entrypoint-initdb.d