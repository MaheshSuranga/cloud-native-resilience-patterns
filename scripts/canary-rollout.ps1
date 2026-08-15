<#
.SYNOPSIS
    Automated Azure Canary Rollout Script with Proportional Traffic Shifting and Health Gates.
.DESCRIPTION
    Executes a multi-stage canary rollout across Azure App Service deployment slots:
    1. Deploys/validates new build to staging slot
    2. Warmup & Health Gate check on staging slot
    3. Shifts 10% of production traffic to staging
    4. Evaluates health gate; shifts 50% traffic
    5. Evaluates health gate; performs zero-downtime atomic slot swap
    6. Clears traffic routing distribution (100% production on new revision)
#>

[CmdletBinding()]
param (
    [string]$ResourceGroup = "rg-resilience-prod",
    [string]$AppName = "resilience-demo-recommendations",
    [string]$SlotName = "staging",
    [string]$TargetSlot = "production",
    [string]$HealthCheckPath = "/health",
    [int]$ObservationWindowSeconds = 5,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host ("=" * 80) -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host ("=" * 80) -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Yellow
}

function Write-ErrorAlert {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Test-HealthGate {
    param([string]$Url)
    Write-Info "Probing Health Gate: $Url"
    if ($DryRun) {
        Write-Success "[DRY RUN] Health gate simulated as HEALTHY (200 OK)"
        return $true
    }
    try {
        $resp = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec 10 -UseBasicParsing
        if ($resp.StatusCode -eq 200) {
            Write-Success "Health Gate PASSED (Status: 200 OK)"
            return $true
        } else {
            Write-ErrorAlert "Health Gate FAILED with status $($resp.StatusCode)"
            return $false
        }
    } catch {
        Write-ErrorAlert "Health Gate unreachable or error: $_"
        return $false
    }
}

function Rollback-Canary {
    Write-ErrorAlert "CRITICAL: Health gate failed! Initiating immediate canary rollback..."
    if (-not $DryRun) {
        az webapp traffic-routing clear --resource-group $ResourceGroup --name $AppName
    }
    Write-Info "Traffic routing rules reset to 0% staging. Production safe."
    exit 1
}

# ------------------------------------------------------------------------------
# START CANARY ROLLOUT PIPELINE
# ------------------------------------------------------------------------------
Write-Step "AZURE CANARY ROLLOUT PIPELINE INITIALIZED"
Write-Info "Resource Group: $ResourceGroup"
Write-Info "App Service:    $AppName"
Write-Info "Staging Slot:   $SlotName"
Write-Info "Target Slot:    $TargetSlot"
Write-Info "DryRun Mode:    $($DryRun.IsPresent)"

$StagingUrl = "https://${AppName}-${SlotName}.azurewebsites.net${HealthCheckPath}"
$ProdUrl = "https://${AppName}.azurewebsites.net${HealthCheckPath}"

# Step 1: Pre-flight Staging Warmup & Health Gate
Write-Step "STEP 1: STAGING SLOT WARMUP & HEALTH GATE VALIDATION"
$healthy = Test-HealthGate -Url $StagingUrl
if (-not $healthy) {
    Rollback-Canary
}

# Step 2: Canary Stage 1 - 10% Traffic Shift
Write-Step "STEP 2: SHIFTING 10% PRODUCTION TRAFFIC TO STAGING"
Write-Info "Executing: az webapp traffic-routing set --distribution ${SlotName}=10"
if (-not $DryRun) {
    az webapp traffic-routing set --resource-group $ResourceGroup --name $AppName --distribution "${SlotName}=10"
}
Write-Success "10% Traffic Shift Applied. Observing health for ${ObservationWindowSeconds}s..."
Start-Sleep -Seconds $ObservationWindowSeconds

$healthy = Test-HealthGate -Url $ProdUrl
if (-not $healthy) {
    Rollback-Canary
}

# Step 3: Canary Stage 2 - 50% Traffic Shift
Write-Step "STEP 3: SHIFTING 50% PRODUCTION TRAFFIC TO STAGING"
Write-Info "Executing: az webapp traffic-routing set --distribution ${SlotName}=50"
if (-not $DryRun) {
    az webapp traffic-routing set --resource-group $ResourceGroup --name $AppName --distribution "${SlotName}=50"
}
Write-Success "50% Traffic Shift Applied. Observing health for ${ObservationWindowSeconds}s..."
Start-Sleep -Seconds $ObservationWindowSeconds

$healthy = Test-HealthGate -Url $ProdUrl
if (-not $healthy) {
    Rollback-Canary
}

# Step 4: Promotion - Zero-Downtime Atomic Slot Swap
Write-Step "STEP 4: PROMOTION - ZERO-DOWNTIME ATOMIC SLOT SWAP"
Write-Info "Executing: az webapp deployment slot swap --slot $SlotName --target-slot $TargetSlot"
if (-not $DryRun) {
    az webapp deployment slot swap --resource-group $ResourceGroup --name $AppName --slot $SlotName --target-slot $TargetSlot
}
Write-Success "Slot swap completed successfully!"

# Step 5: Reset Traffic Routing Rules
Write-Step "STEP 5: CLEARING CANARY ROUTING RULES (100% PRODUCTION)"
Write-Info "Executing: az webapp traffic-routing clear"
if (-not $DryRun) {
    az webapp traffic-routing clear --resource-group $ResourceGroup --name $AppName
}
Write-Success "Traffic routing reset. 100% production traffic routed to updated revision."

# Final Verification
Write-Step "FINAL VERIFICATION: PRODUCTION ENDPOINT HEALTH CHECK"
$finalCheck = Test-HealthGate -Url $ProdUrl
if ($finalCheck) {
    Write-Step "CANARY ROLLOUT COMPLETED WITH 100% SUCCESS!"
} else {
    Write-ErrorAlert "Post-swap verification failed. Please inspect production logs."
}
