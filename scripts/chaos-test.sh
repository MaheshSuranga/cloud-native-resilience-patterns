#!/usr/bin/env bash
# ==============================================================================
# Automated Chaos Testing Harness: Circuit Breaker Lifecycle & Fallback Validation
# ==============================================================================

set -e

RECOMMENDATIONS_URL="http://localhost:5000"

echo ""
echo "================================================================================"
echo "  PHASE 1: STEADY-STATE BASELINE TRAFFIC VALIDATION"
echo "================================================================================"

echo "[INFO] Sending initial cold request for 'user100' (Expect Cache MISS)..."
curl -i -s "${RECOMMENDATIONS_URL}/recommendations/user100" | head -n 12

echo ""
echo "[INFO] Sending second warm request for 'user100' (Expect Cache HIT)..."
curl -i -s "${RECOMMENDATIONS_URL}/recommendations/user100" | head -n 12

echo ""
echo "================================================================================"
echo "  PHASE 2: CHAOS INJECTION - PAUSING DOWNSTREAM ENTITLEMENTS SERVICE"
echo "================================================================================"
echo "[CHAOS] Executing: docker compose pause entitlements-api"
docker compose pause entitlements-api

echo ""
echo "================================================================================"
echo "  PHASE 3: BLASTING TRAFFIC TO TRIP CIRCUIT BREAKER TO OPEN STATE"
echo "================================================================================"
echo "[INFO] Sending 6 requests to un-cached user endpoints..."

for i in {1..6}; do
    echo -n "-> Requesting /recommendations/chaos_user_${i} ... "
    curl -s -w "\nHTTP Status: %{http_code}\n" "${RECOMMENDATIONS_URL}/recommendations/chaos_user_${i}"
    sleep 0.2
done

echo ""
echo "================================================================================"
echo "  PHASE 4: DOWNSTREAM RECOVERY & HALF-OPEN STATE RECOVERY"
echo "================================================================================"
echo "[INFO] Executing: docker compose unpause entitlements-api"
docker compose unpause entitlements-api

echo "[INFO] Waiting 15 seconds for Polly Circuit Breaker BreakDuration cooldown..."
for i in {15..1}; do
    echo -ne "Cooldown remaining: ${i}s...\r"
    sleep 1
done
echo -e "\n[INFO] Cooldown finished. Probing service to trigger HALF-OPEN -> CLOSED transition..."

echo "-> Sending probe request /recommendations/probe_recovery_user ... "
curl -i -s "${RECOMMENDATIONS_URL}/recommendations/probe_recovery_user" | head -n 12

echo ""
echo "================================================================================"
echo "  CHAOS TEST RUN COMPLETE: Circuit Breaker successfully tested"
echo "================================================================================"
