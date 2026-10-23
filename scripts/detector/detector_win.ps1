# Pre-PR check (Windows / PowerShell 7): runs on your machine what the CI will run on the pull request, and shows one table of what
# passed and where it would break. The Linux/macOS twin is detector_mac.sh; both run the same stages and print the same tables.
# See README.md in this folder. Needs: dotnet, git, python, bash (Git for Windows ships it) and, for the integration tests, Docker.
#
# Usage: ./scripts/detector/detector_win.ps1 [-SkipIntegration]
#   -SkipIntegration   do not run the integration tests (no Docker needed); the coverage gate is then skipped too
#
# Keep it in step with .github/workflows/net-core-build-and-test.yml: when a step is added there, add the stage here.
param([switch]$SkipIntegration)
$ErrorActionPreference = "Continue"

# dotnet prints its messages in the language of the machine; the detector reads some of them, so ask for English
$env:DOTNET_CLI_UI_LANGUAGE = "en"
$env:VSLANG = "1033"

$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$out = Join-Path $root "TestResults/detector"
$logs = Join-Path $out "logs"
Set-Location $root

function Write-Info
{
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

$workflow = Get-Content (Join-Path $root ".github/workflows/net-core-build-and-test.yml") -Raw
$threshold = if ($workflow -match "COVERAGE_THRESHOLD:\s*'([0-9.]+)'") { $Matches[1] } else { "99" }

" "
Write-Info "================================================================================"
Write-Info "== DETECTOR => '$root' START"
Write-Info "================================================================================"
" "
Write-Info "## CHECKING THE TOOLS ----------------------------------------------------------"
$python = if (Get-Command python3 -ErrorAction SilentlyContinue) { "python3" } elseif (Get-Command python -ErrorAction SilentlyContinue) { "python" } else { $null }
$missing = $false
foreach ($tool in @("dotnet", "git", "bash"))
{
    if (Get-Command $tool -ErrorAction SilentlyContinue) { Write-Host "$tool found" } else { Write-Host "$tool NOT found" -ForegroundColor Red; $missing = $true }
}
if ($python) { Write-Host "$python found" } else { Write-Host "python NOT found" -ForegroundColor Red; $missing = $true }
if (-not $SkipIntegration)
{
    docker info *> $null
    if ($LASTEXITCODE -eq 0) { Write-Host "docker is running" } else { Write-Host "docker is NOT running (needed by the integration tests; or use -SkipIntegration)" -ForegroundColor Red; $missing = $true }
}
if ($missing)
{
    Write-Host "Fix the above and run again." -ForegroundColor Red
    exit 2
}
" "

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force -Path $logs | Out-Null

$script:results = New-Object System.Collections.Generic.List[object]

function Add-Result
{
    param($Name, $Result, $Time, $Detail)
    $script:results.Add([pscustomobject]@{ Name = $Name; Result = $Result; Time = $Time; Detail = $Detail })
}

# Invoke-Stage: runs the script block, keeps its output in logs/<id>.log, records PASS/FAIL. Returns $true on PASS.
function Invoke-Stage
{
    param([string]$Id, [string]$Title, [scriptblock]$Action)
    Write-Host ("  {0,-36} " -f $Title) -NoNewline
    $watch = [Diagnostics.Stopwatch]::StartNew()
    & $Action *> (Join-Path $logs "$Id.log")
    $ok = ($LASTEXITCODE -eq 0)
    $seconds = [int]$watch.Elapsed.TotalSeconds
    if ($ok)
    {
        Write-Host "PASS" -ForegroundColor Green -NoNewline
        Write-Host "  (${seconds}s)"
        Add-Result $Title "PASS" "${seconds}s" ""
    }
    else
    {
        Write-Host "FAIL" -ForegroundColor Red -NoNewline
        Write-Host "  (${seconds}s)"
        Add-Result $Title "FAIL" "${seconds}s" "TestResults/detector/logs/$Id.log"
    }
    return $ok
}

function Skip-Stage
{
    param([string]$Title, [string]$Reason)
    Write-Host ("  {0,-36} " -f $Title) -NoNewline
    Write-Host "SKIPPED" -ForegroundColor Yellow -NoNewline
    Write-Host "  ($Reason)"
    Add-Result $Title "SKIPPED" "-" $Reason
}

function Get-Reports
{
    param([string]$Kind)
    $folder = Join-Path $out $Kind
    if (-not (Test-Path $folder)) { return @() }
    Get-ChildItem $folder -Recurse -Filter coverage.cobertura.xml | Where-Object { $_.FullName -notmatch '[\\/]In[\\/]' } | ForEach-Object { $_.FullName }
}

function Get-Coverage
{
    param([string[]]$Reports, [string]$Gate = "0")
    & $python scripts/ci-check-coverage.py $Gate @Reports 2>$null
}

$testArgs = @("--no-build", "--configuration", "Release", "--verbosity", "minimal", "--settings", "test/coverlet.runsettings", "--collect:XPlat Code Coverage", "--logger", "trx")

Write-Info "## STAGES ----------------------------------------------------------------------"
$restored = Invoke-Stage "restore" "Restore" { dotnet restore HsnSoft.Base.sln --verbosity minimal }

Invoke-Stage "test-projects" "Every package has a test project" { bash ./scripts/ci-check-test-projects.sh } | Out-Null
if ($restored)
{
    Invoke-Stage "format-cs" "Format (C#)" {
        dotnet format whitespace HsnSoft.Base.sln --verify-no-changes --no-restore
        if ($LASTEXITCODE -eq 0) { dotnet format style HsnSoft.Base.sln --verify-no-changes --severity info --no-restore }
    } | Out-Null
}
else
{
    Skip-Stage "Format (C#)" "restore failed"
}
Invoke-Stage "format-json" "Format (JSON)" { dotnet run scripts/format-json.cs -- --all --check } | Out-Null

$built = $false
if ($restored)
{
    $built = Invoke-Stage "build" "Build (Release, warnings are errors)" {
        dotnet build HsnSoft.Base.sln --no-restore --no-incremental --configuration Release --verbosity minimal -p:TreatWarningsAsErrors=true
    }
}
else
{
    Skip-Stage "Build (Release, warnings are errors)" "restore failed"
}

$unitOk = $false
$integrationOk = $false
if ($built)
{
    $unitOk = Invoke-Stage "unit" "Unit tests" { bash ./scripts/run-tests.sh unit @testArgs --results-directory (Join-Path $out "unit") }
    if ($SkipIntegration)
    {
        Skip-Stage "Integration tests" "-SkipIntegration"
    }
    else
    {
        $integrationOk = Invoke-Stage "integration" "Integration tests" { bash ./scripts/run-tests.sh integration @testArgs --results-directory (Join-Path $out "integration") }
    }
}
else
{
    Skip-Stage "Unit tests" "build did not pass"
    Skip-Stage "Integration tests" "build did not pass"
}

$gateTitle = "Coverage gate (>= $threshold %)"
if ($unitOk -and $integrationOk)
{
    Invoke-Stage "coverage" $gateTitle { & $python scripts/ci-check-coverage.py $threshold @(Get-Reports "unit") @(Get-Reports "integration") } | Out-Null
}
elseif ($SkipIntegration)
{
    Skip-Stage $gateTitle "needs the integration tests"
}
else
{
    Skip-Stage $gateTitle "tests did not pass"
}
" "

# ---- the table ------------------------------------------------------------------------------------------------------------
$failed = @($script:results | Where-Object Result -eq "FAIL").Count
$skipped = @($script:results | Where-Object Result -eq "SKIPPED").Count

Write-Info "## RESULT ----------------------------------------------------------------------"
Write-Host ("  {0,-38} {1,-9} {2,-7} {3}" -f "Stage", "Result", "Time", "Detail")
Write-Host ("  {0,-38} {1,-9} {2,-7} {3}" -f ("-" * 38), ("-" * 9), ("-" * 7), ("-" * 6))
foreach ($row in $script:results)
{
    $color = switch ($row.Result) { "PASS" { "Green" } "FAIL" { "Red" } default { "Yellow" } }
    Write-Host ("  {0,-38} " -f $row.Name) -NoNewline
    Write-Host ("{0,-9}" -f $row.Result) -ForegroundColor $color -NoNewline
    Write-Host (" {0,-7} {1}" -f $row.Time, $row.Detail)
}
" "

# totals from the .trx result files, which are the same in every language
function Get-Counts
{
    param([string]$Kind)
    $sum = [ordered]@{ total = 0; passed = 0; failed = 0; skipped = 0 }
    foreach ($file in (Get-ChildItem (Join-Path $out $Kind) -Recurse -Filter *.trx -ErrorAction SilentlyContinue))
    {
        $counters = ([xml](Get-Content $file.FullName -Raw)).TestRun.ResultSummary.Counters
        $sum.total += [int]$counters.total
        $sum.passed += [int]$counters.passed
        $sum.failed += [int]$counters.failed
        $sum.skipped += [int]$counters.notExecuted
    }
    return $sum
}

if (Test-Path (Join-Path $logs "unit.log"))
{
    Write-Info "## TESTS ----------------------------------------------------------------------"
    Write-Host ("  {0,-20} {1,7} {2,7} {3,7} {4,7}" -f "", "total", "passed", "failed", "skipped")
    foreach ($kind in @("unit", "integration"))
    {
        $log = Join-Path $logs "$kind.log"
        if (-not (Test-Path $log)) { continue }
        $c = Get-Counts $kind
        Write-Host ("  {0,-20} {1,7} {2,7} {3,7} {4,7}" -f $kind, $c.total, $c.passed, $c.failed, $c.skipped)
    }
    " "
}
if ($unitOk)
{
    Write-Info "## LINE COVERAGE (src) ---------------------------------------------------------"
    function Get-LineCoverage { param([string[]]$Reports) ((Get-Coverage $Reports | Select-Object -First 1) -replace '^Line coverage: (.*) \(threshold.*$', '$1') }
    Write-Host "  unit tests only        $(Get-LineCoverage @(Get-Reports 'unit'))"
    if ($integrationOk)
    {
        Write-Host "  integration tests only $(Get-LineCoverage @(Get-Reports 'integration'))"
        Write-Host "  merged (what CI gates) $(Get-LineCoverage (@(Get-Reports 'unit') + @(Get-Reports 'integration')))   threshold $threshold %"
    }
    " "
}

foreach ($row in ($script:results | Where-Object Result -eq "FAIL"))
{
    Write-Info "## $($row.Name): last lines of $($row.Detail)"
    $lines = Get-Content (Join-Path $root $row.Detail) | ForEach-Object { $_ -replace "\x1b\[[0-9;]*m", "" } | Where-Object { $_.Trim() }
    # the lines that say what broke (errors, failed tests and their expected/actual values); the end of the log if there are none
    $picked = $lines | Where-Object { $_ -match ' error |^ *Failed [A-Za-z]|Expected:|Actual:|Error Message|Assert\.|Unable to fix' } | Select-Object -Unique -First 12
    if (-not $picked) { $picked = $lines | Select-Object -Last 12 }
    $picked | ForEach-Object { "    " + $_.Substring(0, [Math]::Min($_.Length, 200)) }
    " "
}

$code = 0
if ($failed -gt 0)
{
    Write-Host "$failed stage(s) would fail in the CI. Fix them and run the detector again before you open the pull request." -ForegroundColor Red
    $code = 1
}
elseif ($skipped -gt 0)
{
    Write-Host "Nothing failed, but $skipped stage(s) were skipped: this is not the full check. Run it without -SkipIntegration before you open the pull request." -ForegroundColor Yellow
}
else
{
    Write-Host "Every stage passed. The pull request is safe to open." -ForegroundColor Green
}
" "
Write-Info "================================================================================"
Write-Info "== DETECTOR FINISHED"
Write-Info "================================================================================"
" "
exit $code
