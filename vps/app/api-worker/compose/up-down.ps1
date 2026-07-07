docker network create -d bridge local.infrastructure-network

docker compose -f compose/docker-compose.yml -p hhs-app-scheduler-local-compose build

docker compose -f compose/docker-compose.yml -p hhs-app-scheduler-local-compose up -d
docker compose -p hhs-app-scheduler-local-compose logs --follow

docker compose -f compose/docker-compose.yml -p hhs-app-scheduler-local-compose down
