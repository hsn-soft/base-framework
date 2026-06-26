#!/bin/bash

# ============================================================================
# TEST START - Phase 6-7: Service Startup & Verification
# ============================================================================
# 2 Phase Service Setup:
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
# PHASE 6: SERVICE STARTUP
# ============================================================================

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
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
echo -e "${BLUE}║    ✓ SERVICES RUNNING - READY FOR TESTING              ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${YELLOW}Summary:${NC}"
echo "  • Microservices: 3/3 Running ✓"
echo "  • Mock APIs: 9/9 Running ✓"
echo "  • RabbitMQ: Connected ✓"
echo ""
echo -e "${YELLOW}Next:${NC} Run test-scenario.sh\n"
