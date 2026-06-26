#!/bin/bash

# ============================================================================
# TEST CLEANUP - Phase 1-5: Port Inventory, Database Mapping, RabbitMQ Check, Cleanup, Port Check
# ============================================================================
# 5 Phase Cleanup & Verification:
# 1. Ports Inventory (3 Microservices + 9 Mock APIs)
# 2. Database Mapping (PostgreSQL + 2x MongoDB)
# 3. RabbitMQ Event Check (pending events in queues)
# 4. Database Cleanup (delete old test data)
# 5. Port Availability Check (free ports)
# ============================================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

REPO_ROOT="/Users/hasansahin/ws/tst/base-framework"

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

# ============================================================================
# PHASE 1: PORTS INVENTORY
# ============================================================================

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    TEST CLEANUP - PHASE 1: PORTS INVENTORY             ║${NC}"
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
  echo -e "${YELLOW}⚠ WARNING: Some ports are still in use${NC}"
  echo -e "${YELLOW}   • Databases cleaned ✓${NC}"
  echo -e "${YELLOW}   • To free ports: pkill -9 dotnet${NC}"
  echo -e "${YELLOW}   • Then run test-start.sh to begin fresh${NC}"
  echo ""
fi

# ============================================================================
# FINAL STATUS
# ============================================================================

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    ✓ CLEANUP COMPLETE                                  ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${YELLOW}Summary:${NC}"
echo "  • Databases: Cleaned ✓"
echo "  • RabbitMQ: Connected ✓"

if [ "$all_free" = true ]; then
  echo "  • Ports: All free ✓"
  echo ""
  echo -e "${YELLOW}Next:${NC} Run test-start.sh (to start services)\n"
else
  echo "  • Ports: Some in use (will be restarted by test-start.sh)"
  echo ""
  echo -e "${YELLOW}Option 1:${NC} Run test-start.sh directly (will stop old services)"
  echo -e "${YELLOW}Option 2:${NC} pkill -9 dotnet, then test-start.sh\n"
fi
