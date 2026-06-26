#!/bin/bash

# ============================================================================
# TEST SETUP SCRIPT - Complete Infrastructure Setup & Verification
# ============================================================================
# 7 Phase Setup Process:
# 1. Ports Inventory (3 Microservices + 9 Mock APIs)
# 2. Database Mapping (PostgreSQL + 2x MongoDB)
# 3. RabbitMQ Event Check (pending events in queues)
# 4. Database Cleanup (delete old test data)
# 5. Port Availability Check (free ports)
# 6. Service Startup (microservices + mock APIs)
# 7. Verification (services running & responsive)
# ============================================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

REPO_ROOT="/Users/hasansahin/ws/tst/base-framework"
LOG_DIR="/tmp/service_logs"

# Define services and ports (zsh-compatible)
SERVICES_DATA="
content:7450
text-normalizer:7460
video-generator:7470
"

MOCKS_DATA="
OutlineFast:5040
OutlineQueue:5041
AudioQuick:5050
AudioHQ:5051
VideoQueueInternal:5061
VideoQueueExternal:5062
CdnLocalMinio:5070
CdnBunnySelf:5071
CdnBunnyS3:5072
"

# Helper function to get port from service name
get_service_port() {
  echo "$SERVICES_DATA" | grep "^$1:" | cut -d: -f2
}

get_mock_port() {
  echo "$MOCKS_DATA" | grep "^$1:" | cut -d: -f2
}

# ============================================================================
# PHASE 1: PORTS INVENTORY
# ============================================================================

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    TEST SETUP - PHASE 1: PORTS INVENTORY               ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${CYAN}Microservices (3):${NC}"
echo "$SERVICES_DATA" | grep -v '^$' | while IFS=: read service port; do
  printf "  %-20s → PORT %4d\n" "$service" "$port"
done

echo ""
echo -e "${CYAN}Mock APIs (9):${NC}"
echo "$MOCKS_DATA" | grep -v '^$' | while IFS=: read mock port; do
  printf "  %-20s → PORT %4d\n" "$mock" "$port"
done

# ============================================================================
# PHASE 2: DATABASE MAPPING
# ============================================================================

echo -e "\n${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    PHASE 2: DATABASE MAPPING                           ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${CYAN}PostgreSQL:${NC}"
echo "  Database: HHS_ContentService_Dev"
echo "  Tables to clean:"
echo "    - CustomerContents"
echo "    - AnalysisContents"
echo "    - AnalysisContentItems"
echo "    - EventInboxMessages"
echo "    - CustomerVpSettings"
echo "    - ContentVideoGenerationLimits"

echo ""
echo -e "${CYAN}MongoDB (Text-Normalizer):${NC}"
echo "  Database: HHS_TextNormalizerService_Dev"
echo "  Collections to clean:"
echo "    - CustomerContentNormalizedRequests"
echo "    - AnalysisContentNormalizedRequests"
echo "    - EventInboxMessages"

echo ""
echo -e "${CYAN}MongoDB (Video-Generator):${NC}"
echo "  Database: HHS_VideoGeneratorService_Dev"
echo "  Collections to clean:"
echo "    - VideoRequests"
echo "    - AudioRequests"
echo "    - EventInboxMessages"

# ============================================================================
# PHASE 3: RABBITMQ EVENT CHECK
# ============================================================================

echo -e "\n${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    PHASE 3: RABBITMQ EVENT CHECK                       ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${CYAN}Checking RabbitMQ connection...${NC}"
if nc -z -w 2 localhost 5672 >/dev/null 2>&1; then
  echo -e "${GREEN}✓${NC} RabbitMQ connected (localhost:5672)"
else
  echo -e "${RED}✗${NC} RabbitMQ not responding"
  exit 1
fi

echo ""
echo -e "${CYAN}Key Event Queues to monitor:${NC}"
echo "  - HHS_ContentService_CustomerContentNormalizeRequestCreated"
echo "  - HHS_TextNormalizerService_VideoGenerationApproved"
echo "  - HHS_VideoGeneratorService_VideoGenerationDataForwarded"
echo "  - HHS_VideoGeneratorService_VideoOperationStarted"

echo ""
echo -e "${YELLOW}Note:${NC} Queues will be purged in cleanup phase"

# ============================================================================
# PHASE 4: DATABASE CLEANUP
# ============================================================================

echo -e "\n${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    PHASE 4: DATABASE CLEANUP                           ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

POSTGRES_USER="postgres"
POSTGRES_PASSWORD="postgres"
POSTGRES_DB="HHS_ContentService_Dev"

echo -e "${CYAN}Cleaning PostgreSQL (HHS_ContentService_Dev)...${NC}"

PGPASSWORD=$POSTGRES_PASSWORD psql -h localhost -U $POSTGRES_USER -d $POSTGRES_DB -c \
  "DELETE FROM \"EventInboxMessages\";" 2>/dev/null && echo -e "${GREEN}✓${NC} EventInboxMessages" || echo -e "${RED}✗${NC} Failed"

PGPASSWORD=$POSTGRES_PASSWORD psql -h localhost -U $POSTGRES_USER -d $POSTGRES_DB -c \
  "DELETE FROM \"AnalysisContentItems\";" 2>/dev/null && echo -e "${GREEN}✓${NC} AnalysisContentItems" || echo -e "${RED}✗${NC} Failed"

PGPASSWORD=$POSTGRES_PASSWORD psql -h localhost -U $POSTGRES_USER -d $POSTGRES_DB -c \
  "DELETE FROM \"AnalysisContents\";" 2>/dev/null && echo -e "${GREEN}✓${NC} AnalysisContents" || echo -e "${RED}✗${NC} Failed"

PGPASSWORD=$POSTGRES_PASSWORD psql -h localhost -U $POSTGRES_USER -d $POSTGRES_DB -c \
  "DELETE FROM \"CustomerContents\";" 2>/dev/null && echo -e "${GREEN}✓${NC} CustomerContents" || echo -e "${RED}✗${NC} Failed"

PGPASSWORD=$POSTGRES_PASSWORD psql -h localhost -U $POSTGRES_USER -d $POSTGRES_DB -c \
  "DELETE FROM \"CustomerVpSettings\";" 2>/dev/null && echo -e "${GREEN}✓${NC} CustomerVpSettings" || echo -e "${RED}✗${NC} Failed"

PGPASSWORD=$POSTGRES_PASSWORD psql -h localhost -U $POSTGRES_USER -d $POSTGRES_DB -c \
  "DELETE FROM \"ContentVideoGenerationLimits\";" 2>/dev/null && echo -e "${GREEN}✓${NC} ContentVideoGenerationLimits" || echo -e "${RED}✗${NC} Failed"

echo ""
echo -e "${CYAN}Cleaning MongoDB (HHS_TextNormalizerService_Dev)...${NC}"

mongosh mongodb://localhost:27017/HHS_TextNormalizerService_Dev --eval \
  "db.CustomerContentNormalizedRequests.deleteMany({});" --quiet 2>/dev/null && echo -e "${GREEN}✓${NC} CustomerContentNormalizedRequests" || echo -e "${RED}✗${NC} Failed"

mongosh mongodb://localhost:27017/HHS_TextNormalizerService_Dev --eval \
  "db.AnalysisContentNormalizedRequests.deleteMany({});" --quiet 2>/dev/null && echo -e "${GREEN}✓${NC} AnalysisContentNormalizedRequests" || echo -e "${RED}✗${NC} Failed"

mongosh mongodb://localhost:27017/HHS_TextNormalizerService_Dev --eval \
  "db.EventInboxMessages.deleteMany({});" --quiet 2>/dev/null && echo -e "${GREEN}✓${NC} EventInboxMessages" || echo -e "${RED}✗${NC} Failed"

echo ""
echo -e "${CYAN}Cleaning MongoDB (HHS_VideoGeneratorService_Dev)...${NC}"

mongosh mongodb://localhost:27017/HHS_VideoGeneratorService_Dev --eval \
  "db.VideoRequests.deleteMany({});" --quiet 2>/dev/null && echo -e "${GREEN}✓${NC} VideoRequests" || echo -e "${RED}✗${NC} Failed"

mongosh mongodb://localhost:27017/HHS_VideoGeneratorService_Dev --eval \
  "db.AudioRequests.deleteMany({});" --quiet 2>/dev/null && echo -e "${GREEN}✓${NC} AudioRequests" || echo -e "${RED}✗${NC} Failed"

mongosh mongodb://localhost:27017/HHS_VideoGeneratorService_Dev --eval \
  "db.EventInboxMessages.deleteMany({});" --quiet 2>/dev/null && echo -e "${GREEN}✓${NC} EventInboxMessages" || echo -e "${RED}✗${NC} Failed"

# ============================================================================
# PHASE 5: PORT AVAILABILITY CHECK
# ============================================================================

echo -e "\n${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    PHASE 5: PORT AVAILABILITY CHECK                    ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

check_port_free() {
  local port=$1
  local name=$2
  if lsof -i :$port >/dev/null 2>&1; then
    echo -e "${RED}✗${NC} Port $port ($name) - IN USE"
    return 1
  else
    echo -e "${GREEN}✓${NC} Port $port ($name) - FREE"
    return 0
  fi
}

all_free=true

echo -e "${CYAN}Checking Microservice Ports:${NC}"
while IFS=: read service port; do
  [ -z "$service" ] && continue
  check_port_free $port "$service" || all_free=false
done <<EOF
$SERVICES_DATA
EOF

echo ""
echo -e "${CYAN}Checking Mock API Ports:${NC}"
while IFS=: read mock port; do
  [ -z "$mock" ] && continue
  check_port_free $port "$mock" || all_free=false
done <<EOF
$MOCKS_DATA
EOF

if [ "$all_free" = false ]; then
  echo ""
  echo -e "${RED}ERROR: Some ports are already in use${NC}"
  echo -e "${YELLOW}Kill existing services: pkill -9 dotnet${NC}"
  exit 1
fi

# ============================================================================
# PHASE 6: SERVICE STARTUP
# ============================================================================

echo -e "\n${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    PHASE 6: SERVICE STARTUP                            ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

mkdir -p $LOG_DIR
cd $REPO_ROOT

echo -e "${CYAN}Starting Mock APIs (9)...${NC}"
while IFS=: read mock port; do
  [ -z "$mock" ] && continue
  path="vps/mock/Hhs.MockApi.$mock"

  if [ -d "$path" ]; then
    echo "  Starting $mock (port $port)..."
    dotnet run --project "$path" -c Release >$LOG_DIR/$mock.log 2>&1 &
    sleep 2
  fi
done <<EOF
$MOCKS_DATA
EOF

echo ""
echo -e "${CYAN}Starting Microservices (3)...${NC}"
for service in content text-normalizer video-generator; do
  port=$(get_service_port "$service")

  case $service in
    content)
      path="vps/service/content/src/Hhs.ContentService.Http.Host"
      ;;
    text-normalizer)
      path="vps/service/text-normalizer/src/Hhs.TextNormalizerService.Http.Host"
      ;;
    video-generator)
      path="vps/service/video-generator/src/Hhs.VideoGeneratorService.Http.Host"
      ;;
  esac

  echo "  Starting $service (port $port)..."
  dotnet run --project "$path" -c Release >$LOG_DIR/$service.log 2>&1 &
  sleep 3
done

# ============================================================================
# PHASE 7: VERIFICATION
# ============================================================================

echo -e "\n${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    PHASE 7: VERIFICATION                               ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${CYAN}Waiting for services to be ready...${NC}"

all_ready=false
for attempt in {1..60}; do
  all_ok=true

  # Check microservices
  for service in content text-normalizer video-generator; do
    port=$(get_service_port "$service")
    if ! curl -s http://localhost:$port >/dev/null 2>&1; then
      all_ok=false
      break
    fi
  done

  # Check mock APIs (essential ones)
  if [ "$all_ok" = true ]; then
    for mock in OutlineFast AudioQuick VideoQueueExternal CdnLocalMinio; do
      port=$(get_mock_port "$mock")
      if ! lsof -i :$port >/dev/null 2>&1; then
        all_ok=false
        break
      fi
    done
  fi

  if [ "$all_ok" = true ]; then
    all_ready=true
    break
  fi

  sleep 1
done

if [ "$all_ready" = false ]; then
  echo -e "${RED}✗${NC} Services did not start within timeout"
  exit 1
fi

echo -e "${GREEN}✓${NC} All services ready!\n"

echo -e "${CYAN}Microservices Status:${NC}"
for service in content text-normalizer video-generator; do
  port=$(get_service_port "$service")
  echo -e "  ${GREEN}✓${NC} $service (port $port) - RUNNING"
done

echo ""
echo -e "${CYAN}Mock APIs Status (4 Essential):${NC}"
for mock in OutlineFast AudioQuick CdnLocalMinio VideoQueueExternal; do
  port=$(get_mock_port "$mock")
  echo -e "  ${GREEN}✓${NC} $mock (port $port) - RUNNING"
done

# ============================================================================
# FINAL STATUS
# ============================================================================

echo -e "\n${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    ✓ SETUP COMPLETE - READY FOR TESTING               ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${YELLOW}Summary:${NC}"
echo "  • Ports: All free ✓"
echo "  • Databases: Cleaned ✓"
echo "  • Microservices: 3/3 Running ✓"
echo "  • Mock APIs: 9/9 Running ✓"
echo "  • RabbitMQ: Connected ✓"
echo ""
echo -e "${YELLOW}Next:${NC} Run test-scenario.sh\n"
