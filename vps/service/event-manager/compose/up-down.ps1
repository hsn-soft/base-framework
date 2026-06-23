docker network create -d bridge local.infrastructure-network

docker-compose -f compose/docker-compose.yml -p hhs-service-event-manager-local-compose build

docker-compose -f compose/docker-compose.yml -p hhs-service-event-manager-local-compose up -d
docker-compose -p hhs-service-event-manager-local-compose logs --follow

docker-compose -f compose/docker-compose.yml -p hhs-service-event-manager-local-compose down
