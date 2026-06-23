docker network create -d bridge local.infrastructure-network

docker-compose -f compose/docker-compose.yml -p hhs-app-commercial-local-compose build

docker-compose -f compose/docker-compose.yml -p hhs-app-commercial-local-compose up -d
docker-compose -p hhs-app-commercial-local-compose logs --follow

docker-compose -f compose/docker-compose.yml -p hhs-app-commercial-local-compose down
