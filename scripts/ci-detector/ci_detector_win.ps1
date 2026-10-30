# Pre-PR check (Windows / PowerShell 7): runs on your machine what the CI will run on the pull request, and shows one table of what
# passed and where it would break. The Linux/macOS twin is ci_detector_mac.sh; both run the same stages and print the same tables.
# See README.md in this folder. Needs: dotnet, git, python, bash (Git for Windows ships it) and, for the integration tests, Docker.
#
# Usage: ./scripts/ci-detector/ci_detector_win.ps1 [-SkipIntegration]
#   -SkipIntegration   do not run the integration tests (no Docker needed); the coverage gate is then skipped too
#
# Keep it in step with .github/workflows/ci-control-workflow.yml: when a step is added there, add the stage here.
param([switch]$SkipIntegration)
$ErrorActionPreference = "Continue"

# dotnet prints its messages in the language of the machine; the detector reads some of them, so ask for English
$env:DOTNET_CLI_UI_LANGUAGE = "en"
$env:VSLANG = "1033"

$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$out = Join-Path $root "TestResults/detector"
$logs = Join-Path $out "logs"
Set-Location $root

function Format-Elapsed
{
    param([TimeSpan]$Span)
    "{0}m {1:00}s" -f [int][math]::Floor($Span.TotalMinutes), $Span.Seconds
}

$detectorStartedAt = Get-Date

function Write-Info
{
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

$workflow = Get-Content (Join-Path $root ".github/workflows/ci-control-workflow.yml") -Raw
$threshold = if ($workflow -match "COVERAGE_THRESHOLD:\s*'([0-9.]+)'") { $Matches[1] } else { "99" }

# a clean screen for the run (only on a terminal, not when the output goes to a file)
if (-not [Console]::IsOutputRedirected) { Clear-Host }

" "
Write-Info "================================================================================"
Write-Info "== DETECTOR => '$root' START ($(Get-Date -Format HH:mm:ss))"
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

# the CI starts from a clean checkout: so does the detector. The cleaning script lists what it deletes; that list goes to the log, the screen gets its summary.
Write-Info "## CLEANING (build output, old test results, editor leftovers) -----------------"
$cleanWatch = [Diagnostics.Stopwatch]::StartNew()
$cleanLog = New-TemporaryFile
& (Join-Path $root "scripts/cleaner/cleaner_win.ps1") *> $cleanLog.FullName
if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE)
{
    Get-Content $cleanLog.FullName
    Write-Host "The cleaning failed; fix the above and run again." -ForegroundColor Red
    exit 2
}
$cleanSeconds = [int]$cleanWatch.Elapsed.TotalSeconds
Get-Content $cleanLog.FullName | ForEach-Object { $_ -replace "\x1b\[[0-9;]*m", "" } | Where-Object { $_ -match 'item\(s\) to delete' } | ForEach-Object { "  $_" }
" "

if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Force -Path $logs | Out-Null
Move-Item $cleanLog.FullName (Join-Path $logs "clean.log")

$script:results = New-Object System.Collections.Generic.List[object]

function Add-Result
{
    param($Name, $Result, $Time, $Detail)
    $script:results.Add([pscustomobject]@{ Name = $Name; Result = $Result; Time = $Time; Detail = $Detail })
}

Add-Result "Clean" "PASS" "${cleanSeconds}s" ""

# Invoke-Stage: runs the script block, keeps its output in logs/<id>.log, records PASS/FAIL. Returns $true on PASS.
# What is shown while a stage runs (its output goes to its log): a line for every test project that finished, a line for every test that passed
# (with its time: the integration tests, which run for minutes) and every test that failed. [Console] writes to the terminal, which the redirection of the stage does not catch.
$script:passed = 0
function Show-Progress
{
    param([string]$Line)
    if ($Line -match '^==> .* completed in ') { [Console]::WriteLine("      " + $Line.Substring(4)) }
    elseif ($Line -match '^  Failed .*\]$') { [Console]::WriteLine("      " + $Line.Trim()) }
    elseif ($Line -match '^  Passed (.*\])$')
    {
        $script:passed++
        $shown = $Matches[1] -replace '^HsnSoft\.Base\.IntegrationTests\.', ''
        if ($shown.Length -gt 110) { $shown = $shown.Substring(0, 110) }
        [Console]::WriteLine(("      {0,3}  {1}" -f $script:passed, $shown))
    }
    $Line
}

function Invoke-Stage
{
    param([string]$Id, [string]$Title, [scriptblock]$Action, [switch]$Live)
    # a stage that shows its progress says that it is running on a line of its own; the result comes on a line of its own after the progress
    if ($Live) { Write-Host ("  {0,-36} running" -f $Title) } else { Write-Host ("  {0,-36} " -f $Title) -NoNewline }
    $watch = [Diagnostics.Stopwatch]::StartNew()
    & $Action *> (Join-Path $logs "$Id.log")
    $ok = ($LASTEXITCODE -eq 0)
    $seconds = [int]$watch.Elapsed.TotalSeconds
    if ($Live) { Write-Host ("  {0,-36} " -f $Title) -NoNewline }
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
    & $python scripts/ci-detector/ci-check-coverage.py $Gate @Reports 2>$null
}

$testArgs = @("--no-build", "--configuration", "Release", "--verbosity", "minimal", "--settings", "test/coverlet.runsettings", "--collect:XPlat Code Coverage", "--logger", "trx")

Write-Info "## STAGES ----------------------------------------------------------------------"
# the rules of the repository need no restore (they read files), so they come first, as in the CI: a broken rule stops before the packages are fetched
Invoke-Stage "test-projects" "Every package has a test project" { bash ./scripts/ci-detector/ci-check-test-project-exist.sh } | Out-Null
Invoke-Stage "referenced-files" "Scripts of the workflows are in git" { bash ./scripts/ci-detector/ci-check-referenced-files.sh } | Out-Null
Invoke-Stage "csproj-order" "Project file reference order" { & $python scripts/ci-detector/ci-force-csproj-order.py } | Out-Null
Invoke-Stage "framework-references" "Framework project references" { & $python scripts/ci-detector/ci-force-framework-references-rules.py } | Out-Null
Invoke-Stage "release-scripts" "Release scripts (their tests)" { & $python -m unittest discover -s scripts/versioning -p "test_*.py" } | Out-Null

$restored = Invoke-Stage "restore" "Restore" { dotnet restore HsnSoft.Base.sln --verbosity minimal }
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
Invoke-Stage "format-json" "Format (JSON)" { dotnet run scripts/ci-detector/format-json.cs -- --all --check } | Out-Null

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
    $unitOk = Invoke-Stage "unit" "Unit tests" { $script:passed = 0; bash ./scripts/run-tests.sh unit @testArgs --results-directory (Join-Path $out "unit") 2>&1 | ForEach-Object { Show-Progress "$_" } } -Live
    if ($SkipIntegration)
    {
        Skip-Stage "Integration tests" "-SkipIntegration"
    }
    else
    {
        $integrationOk = Invoke-Stage "integration" "Integration tests" { $script:passed = 0; bash ./scripts/run-tests.sh integration @testArgs --logger "console;verbosity=normal" --results-directory (Join-Path $out "integration") 2>&1 | ForEach-Object { Show-Progress "$_" } } -Live
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
    Invoke-Stage "coverage" $gateTitle { & $python scripts/ci-detector/ci-check-coverage.py $threshold @(Get-Reports "unit") @(Get-Reports "integration") } | Out-Null
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
Write-Host ("  {0,-38} {1,-9} {2}" -f "Total", "", (Format-Elapsed ((Get-Date) - $detectorStartedAt)))
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
Write-Info "== DETECTOR FINISHED ($(Get-Date -Format HH:mm:ss), total $(Format-Elapsed ((Get-Date) - $detectorStartedAt)))"
Write-Info "================================================================================"
" "
exit $code
