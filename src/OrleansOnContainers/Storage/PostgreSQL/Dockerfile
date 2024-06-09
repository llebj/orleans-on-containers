# syntax=docker/dockerfile:1

ARG PostgresVersion

FROM postgres:${PostgresVersion} AS base
ENV ORLEANS_SCRIPTS_DIR=/scripts
WORKDIR /
COPY Scripts scripts
COPY pg-init.sh docker-entrypoint-initdb.d