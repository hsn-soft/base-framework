docker network create -d bridge local.infrastructure-network

docker-compose -f compose/docker-compose.yml -p hhs-service-text-normalizer-local-compose build

docker-compose -f compose/docker-compose.yml -p hhs-service-text-normalizer-local-compose up -d
docker-compose -p hhs-service-text-normalizer-local-compose logs --follow

docker-compose -f compose/docker-compose.yml -p hhs-service-text-normalizer-local-compose down
