# ==============================================================================
# Automated Chaos Testing Harness: Circuit Breaker Lifecycle & Fallback Validation
# ==============================================================================

$ErrorActionPreference = "Continue"

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Text)
    Write-Host "[SUCCESS] $Text" -ForegroundColor Green
}

function Write-Info {
    param([string]$Text)
    Write-Host "[INFO] $Text" -ForegroundColor Yellow
}

function Write-Alert {
    param([string]$Text)
    Write-Host "[CHAOS] $Text" -ForegroundColor Red
}

$RecommendationsUrl = "http://localhost:5000"

# ------------------------------------------------------------------------------
# Phase 1: Steady-State Baseline Traffic
# ------------------------------------------------------------------------------
Write-Header "PHASE 1: STEADY-STATE BASELINE TRAFFIC VALIDATION"

Write-Info "Sending initial cold request for 'user100' (Expect Cache MISS)..."
try {
    $resp1 = Invoke-WebRequest -Uri "$RecommendationsUrl/recommendations/user100" -Method Get -UseBasicParsing
    $cacheHeader1 = $resp1.Headers["X-Cache"]
    Write-Success "Response 1 Status: $($resp1.StatusCode), X-Cache: $cacheHeader1"
    Write-Host "Payload: $($resp1.Content)" -ForegroundColor DarkGray
} catch {
    Write-Host "Error in Phase 1 (Request 1): $_" -ForegroundColor Red
}

Write-Info "Sending second warm request for 'user100' (Expect Cache HIT)..."
try {
    $resp2 = Invoke-WebRequest -Uri "$RecommendationsUrl/recommendations/user100" -Method Get -UseBasicParsing
    $cacheHeader2 = $resp2.Headers["X-Cache"]
    Write-Success "Response 2 Status: $($resp2.StatusCode), X-Cache: $cacheHeader2"
    Write-Host "Payload: $($resp2.Content)" -ForegroundColor DarkGray
} catch {
    Write-Host "Error in Phase 1 (Request 2): $_" -ForegroundColor Red
}

# ------------------------------------------------------------------------------
# Phase 2: Chaos Injection (Pause Entitlements Service)
# ------------------------------------------------------------------------------
Write-Header "PHASE 2: CHAOS INJECTION - PAUSING DOWNSTREAM ENTITLEMENTS SERVICE"
Write-Alert "Executing: docker compose pause entitlements-api"
docker compose pause entitlements-api

# ------------------------------------------------------------------------------
# Phase 3: Blast Requests to Force Circuit Breaker OPEN
# ------------------------------------------------------------------------------
Write-Header "PHASE 3: BLASTING TRAFFIC TO TRIP CIRCUIT BREAKER TO OPEN STATE"
Write-Info "Sending 6 requests to un-cached user endpoints..."

1..6 | ForEach-Object {
    $userId = "chaos_user_$_"
    Write-Host "-> Requesting /recommendations/$userId ..." -NoNewline
    try {
        $resp = Invoke-WebRequest -Uri "$RecommendationsUrl/recommendations/$userId" -Method Get -TimeoutSec 4 -UseBasicParsing
        Write-Host " Status: $($resp.StatusCode)" -ForegroundColor Green
    } catch {
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            $retryAfter = $_.Exception.Response.Headers["Retry-After"]
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $body = $reader.ReadToEnd()
            Write-Host " Status: $statusCode (Fallback Active, Retry-After: $retryAfter)" -ForegroundColor Red
            Write-Host "   Payload: $body" -ForegroundColor DarkRed
        } else {
            Write-Host " Network Error / Timeout: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    Start-Sleep -Milliseconds 200
}

# ------------------------------------------------------------------------------
# Phase 4: Recovery & Half-Open Transition
# ------------------------------------------------------------------------------
Write-Header "PHASE 4: DOWNSTREAM RECOVERY & HALF-OPEN STATE RECOVERY"
Write-Info "Executing: docker compose unpause entitlements-api"
docker compose unpause entitlements-api

Write-Info "Waiting 15 seconds for Polly Circuit Breaker BreakDuration cooldown..."
for ($i = 15; $i -gt 0; $i--) {
    Write-Host "Cooldown remaining: ${i}s..." -NoNewline
    Start-Sleep -Seconds 1
    Write-Host "`r" -NoNewline
}
Write-Host "`nCooldown finished. Probing service to trigger HALF-OPEN -> CLOSED transition..."

try {
    $recoveryResp = Invoke-WebRequest -Uri "$RecommendationsUrl/recommendations/probe_recovery_user" -Method Get -UseBasicParsing
    Write-Success "Probe Request Succeeded! Status: $($recoveryResp.StatusCode), X-Cache: $($recoveryResp.Headers['X-Cache'])"
    Write-Host "Payload: $($recoveryResp.Content)" -ForegroundColor DarkGray
    Write-Success "Circuit Breaker transitioned to CLOSED state. System operating normally!"
} catch {
    Write-Host "Recovery Probe Failed: $_" -ForegroundColor Red
}

Write-Header "CHAOS TEST RUN COMPLETE"
