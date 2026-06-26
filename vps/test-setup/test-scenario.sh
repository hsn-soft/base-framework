#!/bin/bash

# ============================================================================
# TEST SCENARIO EXECUTION - Customer Content + Analysis Content
# ============================================================================
# Prerequisites: test-setup.sh must complete successfully
#
# Features:
# - Uses {{$timestamp}} for unique ContentKey
# - Includes X-Correlation-Id header with timestamp
# - Extracts successful customer-content IDs
# - Uses extracted IDs for analysis-content scenarios
# - Validates: NormalizeStatus=COMPLETED AND VideoStatus=COMPLETED
# ============================================================================

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

# Configuration
CONTENT_SERVICE_URL="http://localhost:7450"
POSTGRES_USER="postgres"
POSTGRES_PASSWORD="postgres"
POSTGRES_DB="HHS_ContentService_Dev"

TEST_TIMEOUT_SECONDS=180
POLL_INTERVAL_SECONDS=2

# ============================================================================
# CUSTOMER CONTENT TEST SCENARIOS (001-006)
# ============================================================================

declare -a CUSTOMER_SCENARIOS=(
  "scenario-001"
  "scenario-002"
  "scenario-003"
  "scenario-004"
  "scenario-005"
  "scenario-006"
)

# ============================================================================
# FUNCTIONS
# ============================================================================

check_completion() {
  local content_id=$1

  RESULT=$(PGPASSWORD=$POSTGRES_PASSWORD psql -h localhost -U $POSTGRES_USER \
    -d $POSTGRES_DB -t -c \
    "SELECT \"NormalizeStatus\" || '|' || \"VideoStatus\" FROM \"CustomerContents\" WHERE \"Id\" = '$content_id';" 2>/dev/null || echo "|")

  NORM=$(echo "$RESULT" | cut -d'|' -f1 | xargs)
  VID=$(echo "$RESULT" | cut -d'|' -f2 | xargs)

  echo "$NORM|$VID"
}

run_customer_scenario() {
  local scenario_name=$1
  local timestamp=$(date +%s%N | cut -c1-13)  # milliseconds
  local correlation_id="$scenario_name-$timestamp"
  local content_key="/customer/$scenario_name-$timestamp"
  local domain="https://scenario.com"

  echo -e "${YELLOW}[Scenario]${NC} Creating $scenario_name..."
  echo "  CorrelationId: $correlation_id"

  # POST with timestamp in ContentKey and X-Correlation-Id header
  RESPONSE=$(curl -s -X POST "$CONTENT_SERVICE_URL/api/content-service/v1/commercial/tests/customer-contents" \
    -H "Content-Type: application/json" \
    -H "X-Correlation-Id: $correlation_id" \
    -d "{
      \"scopeKey\": \"$scenario_name\",
      \"domainName\": \"$domain\",
      \"contentKey\": \"$content_key\"
    }")

  CONTENT_ID=$(echo $RESPONSE | jq -r '.payload.id' 2>/dev/null)

  if [ -z "$CONTENT_ID" ] || [ "$CONTENT_ID" = "null" ]; then
    echo -e "${RED}✗${NC} Failed to create content"
    echo "  Response: $RESPONSE"
    return 1
  fi

  echo "  ContentId: $CONTENT_ID"

  # Monitor until completion
  local start_time=$(date +%s)
  local success=false

  while true; do
    local current_time=$(date +%s)
    local elapsed=$((current_time - start_time))

    STATUS=$(check_completion "$CONTENT_ID")
    NORM=$(echo "$STATUS" | cut -d'|' -f1)
    VID=$(echo "$STATUS" | cut -d'|' -f2)

    if [ "$NORM" = "COMPLETED" ] && [ "$VID" = "COMPLETED" ]; then
      echo -e "  ${GREEN}✓${NC} [$elapsed s] COMPLETED"
      success=true
      break
    fi

    if [ $elapsed -gt $TEST_TIMEOUT_SECONDS ]; then
      echo -e "  ${RED}✗${NC} [$elapsed s] TIMEOUT (NormalizeStatus=$NORM | VideoStatus=$VID)"
      break
    fi

    if [ $((elapsed % 10)) -eq 0 ]; then
      printf "  [%d s] NormalizeStatus: %-12s | VideoStatus: %-12s\n" "$elapsed" "$NORM" "$VID"
    fi

    sleep $POLL_INTERVAL_SECONDS
  done

  if [ "$success" = true ]; then
    echo "$CONTENT_ID"
    return 0
  else
    return 1
  fi
}

# ============================================================================
# MAIN EXECUTION - CUSTOMER CONTENT TESTS
# ============================================================================

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    CUSTOMER CONTENT TEST SCENARIOS (001-006)           ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

declare -a CUSTOMER_CONTENT_IDS=()
CUSTOMER_PASSED=0
CUSTOMER_FAILED=0

for scenario_name in "${CUSTOMER_SCENARIOS[@]}"; do
  CONTENT_ID=$(run_customer_scenario "$scenario_name")

  if [ $? -eq 0 ] && [ ! -z "$CONTENT_ID" ]; then
    CUSTOMER_CONTENT_IDS+=("$CONTENT_ID")
    ((CUSTOMER_PASSED++))
  else
    ((CUSTOMER_FAILED++))
  fi

  echo ""
done

echo -e "${BLUE}════════════════════════════════════════════════════════${NC}"
echo -e "Customer Content Results: ${GREEN}$CUSTOMER_PASSED passed${NC} | ${RED}$CUSTOMER_FAILED failed${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════${NC}\n"

if [ $CUSTOMER_FAILED -gt 0 ]; then
  echo -e "${RED}✗ Customer Content tests FAILED${NC}"
  echo -e "${YELLOW}Check test-setup.sh completed all phases${NC}\n"
  exit 1
fi

if [ $CUSTOMER_PASSED -lt 6 ]; then
  echo -e "${YELLOW}⚠ Only $CUSTOMER_PASSED/6 customer content scenarios passed${NC}\n"
fi

# ============================================================================
# ANALYSIS CONTENT TEST SCENARIOS (001-006)
# ============================================================================

if [ $CUSTOMER_PASSED -eq 0 ]; then
  echo -e "${YELLOW}Skipping Analysis Content tests - no customer content IDs available${NC}\n"
  exit 0
fi

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    ANALYSIS CONTENT TEST SCENARIOS (001-006)           ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

# Group customer content IDs into analysis sets
# Analysis-001: first 3, Analysis-002: next 3, etc.
declare -a ANALYSIS_GROUPS=()
for ((i=0; i<${#CUSTOMER_CONTENT_IDS[@]}; i+=3)); do
  GROUP=()
  for ((j=0; j<3 && i+j<${#CUSTOMER_CONTENT_IDS[@]}; j++)); do
    GROUP+=("${CUSTOMER_CONTENT_IDS[i+j]}")
  done
  if [ ${#GROUP[@]} -gt 0 ]; then
    ANALYSIS_GROUPS+=("${GROUP[@]}")
  fi
done

ANALYSIS_PASSED=0
ANALYSIS_FAILED=0

for scenario_num in {1..6}; do
  scenario_name="scenario-$(printf '%03d' $scenario_num)"
  timestamp=$(date +%s%N | cut -c1-13)
  correlation_id="$scenario_name-$timestamp"

  echo -e "${YELLOW}[Scenario]${NC} Creating $scenario_name..."
  echo "  CorrelationId: $correlation_id"

  # Check if we have any successful customer content for this scope
  if [ ${#CUSTOMER_CONTENT_IDS[@]} -eq 0 ]; then
    echo -e "${YELLOW}⚠${NC} Skipping analysis - no customer content IDs available"
    echo ""
    continue
  fi

  # POST with NEW ENDPOINT - auto-fetches latest 5 successful CustomerContents by ScopeKey
  RESPONSE=$(curl -s -X POST "$CONTENT_SERVICE_URL/api/content-service/v1/commercial/tests/analysis-contents-from-scope" \
    -H "Content-Type: application/json" \
    -H "X-Correlation-Id: $correlation_id" \
    -d "{
      \"scopeKey\": \"$scenario_name\",
      \"domainName\": \"https://scenario.com\",
      \"title\": \"Analysis $scenario_name - $timestamp\",
      \"maxCustomerContents\": 5
    }")

  ANALYSIS_ID=$(echo $RESPONSE | jq -r '.payload.id' 2>/dev/null)

  if [ -z "$ANALYSIS_ID" ] || [ "$ANALYSIS_ID" = "null" ]; then
    echo -e "${RED}✗${NC} Failed to create analysis"
    echo "  Response: $RESPONSE"
    ((ANALYSIS_FAILED++))
    echo ""
    continue
  fi

  echo "  AnalysisId: $ANALYSIS_ID"
  echo "  (Auto-fetching latest 5 successful CustomerContents by ScopeKey)"

  # Monitor until completion
  start_time=$(date +%s)
  success=false

  while true; do
    current_time=$(date +%s)
    elapsed=$((current_time - start_time))

    RESULT=$(PGPASSWORD=$POSTGRES_PASSWORD psql -h localhost -U $POSTGRES_USER \
      -d $POSTGRES_DB -t -c \
      "SELECT \"NormalizeStatus\" || '|' || \"VideoStatus\" FROM \"AnalysisContents\" WHERE \"Id\" = '$ANALYSIS_ID';" 2>/dev/null || echo "|")

    NORM=$(echo "$RESULT" | cut -d'|' -f1 | xargs)
    VID=$(echo "$RESULT" | cut -d'|' -f2 | xargs)

    if [ "$NORM" = "COMPLETED" ] && [ "$VID" = "COMPLETED" ]; then
      echo -e "  ${GREEN}✓${NC} [$elapsed s] COMPLETED"
      ((ANALYSIS_PASSED++))
      success=true
      break
    fi

    if [ $elapsed -gt $TEST_TIMEOUT_SECONDS ]; then
      echo -e "  ${RED}✗${NC} [$elapsed s] TIMEOUT (NormalizeStatus=$NORM | VideoStatus=$VID)"
      ((ANALYSIS_FAILED++))
      break
    fi

    if [ $((elapsed % 10)) -eq 0 ]; then
      printf "  [%d s] NormalizeStatus: %-12s | VideoStatus: %-12s\n" "$elapsed" "$NORM" "$VID"
    fi

    sleep $POLL_INTERVAL_SECONDS
  done

  echo ""
done

echo -e "${BLUE}════════════════════════════════════════════════════════${NC}"
echo -e "Analysis Content Results: ${GREEN}$ANALYSIS_PASSED passed${NC} | ${RED}$ANALYSIS_FAILED failed${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════════${NC}\n"

# ============================================================================
# FINAL SUMMARY
# ============================================================================

echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║    TEST EXECUTION SUMMARY                             ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}\n"

echo -e "${CYAN}Customer Content:${NC} ${GREEN}$CUSTOMER_PASSED/6 passed${NC}"
echo -e "${CYAN}Analysis Content:${NC} ${GREEN}$ANALYSIS_PASSED/6 passed${NC}"
echo ""

if [ $CUSTOMER_FAILED -eq 0 ] && [ $ANALYSIS_FAILED -eq 0 ]; then
  echo -e "${GREEN}✓ ALL TESTS PASSED${NC}\n"
  exit 0
else
  echo -e "${RED}✗ SOME TESTS FAILED${NC}\n"
  exit 1
fi
