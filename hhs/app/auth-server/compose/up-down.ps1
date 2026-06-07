docker network create -d bridge local.infrastructure-network

docker-compose -f compose/docker-compose.yml -p hhs-app-auth-server-local-compose build

docker-compose -f compose/docker-compose.yml -p hhs-app-auth-server-local-compose up -d
docker-compose -p hhs-app-auth-server-local-compose logs --follow

docker-compose -f compose/docker-compose.yml -p hhs-app-auth-server-local-compose down
