# Local SonarQube analysis (Windows / PowerShell 7). The Linux/macOS twin is analysis_mac.sh; both do the same steps and print the same messages.
# See README.md in this folder.
#
# Usage:    ./scripts/sonar/analysis_win.ps1      (SONAR_TOKEN comes from scripts/sonar/.env, or from the environment)
# Optional: $env:SONAR_HOST_URL (default http://localhost:8031), $env:SONAR_PROJECT_KEY (default hsnsoft-base-framework)
$ErrorActionPreference = "Stop"

function Write-Info
{
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Assert-LastExitCode
{
    param([string]$Step)
    if ($LASTEXITCODE -ne 0)
    {
        throw "$Step failed (exit code $LASTEXITCODE)"
    }
}

# Settings from the git-ignored .env next to this script; a variable already set in the environment wins.
$envFile = Join-Path $PSScriptRoot ".env"
if (Test-Path $envFile)
{
    foreach ($line in Get-Content $envFile)
    {
        if ($line -match '^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*"?(.*?)"?\s*$' -and -not [Environment]::GetEnvironmentVariable($Matches[1]))
        {
            [Environment]::SetEnvironmentVariable($Matches[1], $Matches[2])
        }
    }
}

if (-not $env:SONAR_TOKEN)
{
    throw "SONAR_TOKEN is required (put it in scripts/sonar/.env, see .env.example; a user token from SonarQube: My Account > Security)"
}
$sonarHostUrl = if ($env:SONAR_HOST_URL) { $env:SONAR_HOST_URL } else { "http://localhost:8031" }
$sonarProjectKey = if ($env:SONAR_PROJECT_KEY) { $env:SONAR_PROJECT_KEY } else { "hsnsoft-base-framework" }

$rootFolder = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$testOutputDir = Join-Path $rootFolder "TestResults"
$env:PATH += [IO.Path]::PathSeparator + (Join-Path $HOME ".dotnet/tools")
Set-Location $rootFolder

" "
Write-Info "================================================================================"
Write-Info "== PROJECT FOLDER => '$rootFolder' START"
Write-Info "================================================================================"
" "
Write-Info "## CHECKING THE ENVIRONMENT ----------------------------------------------------"
try
{
    $status = Invoke-RestMethod "$sonarHostUrl/api/system/status"
}
catch
{
    throw "SonarQube is not reachable at $sonarHostUrl (start it: docker compose -f docker/sonarqube.compose.yml up -d)"
}
if ($status.status -ne "UP")
{
    throw "SonarQube at $sonarHostUrl is not ready (status: $($status.status))"
}
Write-Host "SonarQube is up at $sonarHostUrl"
docker info *> $null
if ($LASTEXITCODE -ne 0)
{
    throw "Docker is not running: the integration tests will fail"
}
Write-Host "Docker is running (the integration tests need it)"
" "
Write-Info "## SONAR SCANNER INSTALL -------------------------------------------------------"
if (dotnet tool list -g | Select-String "dotnet-sonarscanner")
{
    Write-Host "dotnet-sonarscanner already installed"
}
else
{
    dotnet tool install --global dotnet-sonarscanner
    Assert-LastExitCode "dotnet-sonarscanner install"
}
Write-Info "## SONAR SCANNER INSTALL COMPLETED ---------------------------------------------"
" "
Write-Info "## TEST FOLDER CLEANING --------------------------------------------------------"
if (Test-Path $testOutputDir)
{
    Write-Host "Cleaning temporary Test Output path $testOutputDir"
    Remove-Item $testOutputDir -Recurse -Force
}
Write-Info "## TEST FOLDER CLEANING COMPLETED ----------------------------------------------"
" "
Write-Info "## SONAR SCANNER ANALYSIS ------------------------------------------------------"
dotnet sonarscanner begin `
    /k:"$sonarProjectKey" `
    /d:sonar.host.url="$sonarHostUrl" `
    /d:sonar.token="$env:SONAR_TOKEN" `
    /d:sonar.cs.vstest.reportsPaths="$testOutputDir/**/*.trx" `
    /d:sonar.cs.opencover.reportsPaths="$testOutputDir/**/coverage.opencover.xml" `
    /d:sonar.coverage.exclusions="test/**,samples/**,scripts/**,**/Migrations/**,**/*Test*.cs,**/*Test*/**,**/tmp/**,**/Tmp/**,**/TMP/**" `
    /d:sonar.inclusions="**/*.cs,**/*.js,**/*.css" `
    /d:sonar.exclusions="test/**,samples/**,scripts/**,**/Migrations/**,**/*Test*.cs,**/*Test*/**,**/tmp/**,**/Tmp/**,**/TMP/**,**/data/**,**/logs/**,**/sample/**"
Assert-LastExitCode "sonarscanner begin"
" "
Write-Info "## DOTNET RESTORE --------------------------------------------------------------"
dotnet restore --force --verbosity minimal
Assert-LastExitCode "dotnet restore"
" "
Write-Info "## DOTNET BUILD ----------------------------------------------------------------"
dotnet build --no-restore --no-incremental --verbosity minimal -c Release
Assert-LastExitCode "dotnet build"
" "
Write-Info "## DOTNET TEST -----------------------------------------------------------------"
$testProjects = Get-ChildItem -Path (Join-Path $rootFolder "test") -Recurse -Filter *.csproj
foreach ($proj in $testProjects)
{
    $projName = $proj.BaseName
    $outDir = Join-Path $testOutputDir $projName
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null

    " "
    Write-Info "## TEST [ $projName ]"
    " "
    dotnet test $proj.FullName `
        --no-build --no-restore --configuration Release `
        --settings (Join-Path $rootFolder "test/coverlet.runsettings") `
        --collect:"XPlat Code Coverage" `
        --results-directory $outDir `
        --logger "trx;LogFileName=$projName.trx"
    Assert-LastExitCode "tests of $projName"
}
" "
Write-Info "## DOTNET TEST COMPLETED -------------------------------------------------------"
" "
Write-Info "## SONAR SCANNER CHECKING RESULTS ----------------------------------------------"
dotnet sonarscanner end /d:sonar.token="$env:SONAR_TOKEN"
Assert-LastExitCode "sonarscanner end"
" "
Write-Info "## SONAR SCANNER ANALYSIS COMPLETED --------------------------------------------"
Write-Info "## DASHBOARD => $sonarHostUrl/dashboard?id=$sonarProjectKey"
" "
Write-Info "================================================================================"
Write-Info "== PROJECT FOLDER => '$rootFolder' FINISHED"
Write-Info "================================================================================"
" "
