#!/bin/sh
set -eux

docker compose -f "docker-compose-local.yml" --env-file ".env.local" "$@"
