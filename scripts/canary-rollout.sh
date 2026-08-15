#!/usr/bin/env bash
# ==============================================================================
# Automated Azure Canary Rollout Script with Proportional Traffic Shifting
# ==============================================================================

set -eo pipefail

RESOURCE_GROUP="${1:-rg-resilience-prod}"
APP_NAME="${2:-resilience-demo-recommendations}"
SLOT_NAME="${3:-staging}"
TARGET_SLOT="${4:-production}"
HEALTH_CHECK_PATH="${5:-/health}"
OBSERVATION_SECONDS="${6:-5}"

STAGING_URL="https://${APP_NAME}-${SLOT_NAME}.azurewebsites.net${HEALTH_CHECK_PATH}"
PROD_URL="https://${APP_NAME}.azurewebsites.net${HEALTH_CHECK_PATH}"

echo "================================================================================"
echo "  AZURE CANARY ROLLOUT PIPELINE INITIALIZED"
echo "================================================================================"
echo "[INFO] Resource Group: $RESOURCE_GROUP"
echo "[INFO] App Service:    $APP_NAME"
echo "[INFO] Staging Slot:   $SLOT_NAME"
echo "[INFO] Target Slot:    $TARGET_SLOT"

test_health_gate() {
    local url="$1"
    echo "[INFO] Probing health gate: $url"
    local status_code
    status_code=$(curl -s -o /dev/null -w "%{http_code}" "$url" || echo "000")
    if [ "$status_code" -eq 200 ]; then
        echo "[SUCCESS] Health Gate PASSED (Status: 200 OK)"
        return 0
    else
        echo "[ERROR] Health Gate FAILED (Status: $status_code)"
        return 1
    fi
}

rollback_canary() {
    echo "[ERROR] Critical failure detected! Rolling back canary traffic distribution..."
    az webapp traffic-routing clear --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" || true
    echo "[INFO] Traffic routing reset to 0% staging. Production safe."
    exit 1
}

# Step 1: Pre-flight Staging Warmup
echo ""
echo "================================================================================"
echo "  STEP 1: STAGING SLOT WARMUP & HEALTH GATE VALIDATION"
echo "================================================================================"
if ! test_health_gate "$STAGING_URL"; then
    rollback_canary
fi

# Step 2: 10% Traffic Shift
echo ""
echo "================================================================================"
echo "  STEP 2: SHIFTING 10% PRODUCTION TRAFFIC TO STAGING"
echo "================================================================================"
az webapp traffic-routing set --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --distribution "${SLOT_NAME}=10"
echo "[SUCCESS] 10% traffic shifted to staging. Observing for ${OBSERVATION_SECONDS}s..."
sleep "$OBSERVATION_SECONDS"

if ! test_health_gate "$PROD_URL"; then
    rollback_canary
fi

# Step 3: 50% Traffic Shift
echo ""
echo "================================================================================"
echo "  STEP 3: SHIFTING 50% PRODUCTION TRAFFIC TO STAGING"
echo "================================================================================"
az webapp traffic-routing set --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --distribution "${SLOT_NAME}=50"
echo "[SUCCESS] 50% traffic shifted to staging. Observing for ${OBSERVATION_SECONDS}s..."
sleep "$OBSERVATION_SECONDS"

if ! test_health_gate "$PROD_URL"; then
    rollback_canary
fi

# Step 4: Promotion (Zero-Downtime Atomic Slot Swap)
echo ""
echo "================================================================================"
echo "  STEP 4: PROMOTION - ZERO-DOWNTIME ATOMIC SLOT SWAP"
echo "================================================================================"
az webapp deployment slot swap --resource-group "$RESOURCE_GROUP" --name "$APP_NAME" --slot "$SLOT_NAME" --target-slot "$TARGET_SLOT"
echo "[SUCCESS] Atomic slot swap completed successfully!"

# Step 5: Clear Routing Rules
echo ""
echo "================================================================================"
echo "  STEP 5: CLEARING CANARY ROUTING RULES (100% PRODUCTION)"
echo "================================================================================"
az webapp traffic-routing clear --resource-group "$RESOURCE_GROUP" --name "$APP_NAME"
echo "[SUCCESS] Traffic routing reset. 100% traffic on upgraded revision."

echo ""
echo "================================================================================"
echo "  CANARY ROLLOUT COMPLETED WITH 100% SUCCESS!"
echo "================================================================================"
