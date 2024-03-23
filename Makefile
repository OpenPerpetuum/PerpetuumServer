
local_dockerfile="docker-compose-local.yml"
compose="docker compose -f '$(local_dockerfile)'" "$@"


help:
	echo Commands to compile, deploy, log services used to run an OpenPerpetuum server (used for local development)

compose-up:
	./script/compose.sh up -d --build --remove-orphans --wait

compose-down:
	./script/compose.sh down --volumes

compose-restart: compose-down compose-up

log-asset:
	./script/compose.sh logs asset -f

log-db:
	./script/compose.sh logs db -f

log-server:
	./script/compose.sh logs server -f

phonyx: help compose-start compose-stop compose-down log-asset log-db log-server
