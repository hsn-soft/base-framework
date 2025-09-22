# INFO Logger Fonksiyonu
function Write-Info
{
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

#Clear screen
clear

$rootFolder = (Get-Item -Path "./" -Verbose).FullName
" "
Write-Info "================================================================================"
Write-Info "== PROJECT FOLDER => '$rootFolder' START"
Write-Info "================================================================================"
" "
Write-Info "## SONAR SCANNER INSTALL -------------------------------------------------------"
" "
$x = dotnet tool list -g | Select-String "dotnet-sonarscanner"
if (-not $x)
{
    dotnet tool install --global dotnet-sonarscanner
}
else
{
    Write-Host "dotnet-sonarscanner already installed"
}
" "
Write-Info "## SONAR SCANNER INSTALL COMPLETED ---------------------------------------------"
" "
Write-Info "## TEST FOLDER CLEANING --------------------------------------------------------"
$testOutputDir = "$rootFolder/TestResults"
if (Test-Path $testOutputDir)
{
    " "
    Write-host "Cleaning temporary Test Output path $testOutputDir"
    Remove-Item $testOutputDir -Recurse -Force
}
" "
Write-Info "## TEST FOLDER CLEANING COMPLETED ----------------------------------------------"
" "
Write-Info "## SONAR SCANNER ANALYSIS ------------------------------------------------------"
" "
## SONAR BEGIN -------------------------------------------------------------------------------
dotnet sonarscanner begin `
    /k:"data-project" `
    /d:sonar.host.url="http://localhost:8031" `
    /d:sonar.login="`sqp_a2947406f6a418abc08d5dfc3fac5dde1e1efc8c" `
    /o:"hsnsh" `
    /d:sonar.cs.vstest.reportsPaths="$rootFolder/TestResults/**/*.trx" `
    /d:sonar.cs.opencover.reportsPaths="$rootFolder/TestResults/**/coverage.opencover.xml" `
    /d:sonar.coverage.exclusions="**/*Test*.cs" `
    /d:sonar.inclusions="**/*.cs,**/*.js,**/*.css" `
    /d:sonar.exclusions="**/data/**,**/logs/**,**/sample/**"
" "
Write-Info "## DOTNET RESTORE --------------------------------------------------------------"
" "
dotnet restore --force --verbosity minimal
" "
Write-Info "## DOTNET BUILD ----------------------------------------------------------------"
" "
dotnet build --no-restore --no-incremental --verbosity minimal -c release
" "
Write-Info "## DOTNET TEST -----------------------------------------------------------------"
$testProjects = Get-ChildItem -Path "$rootFolder/test" -Recurse -Filter *.csproj
foreach ($proj in $testProjects)
{
    $projName = $proj.BaseName
    $outDir = Join-Path $testOutputDir $projName
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null

    " "
    Write-Info "## TEST [ $projName ] "
    " "
    dotnet test $proj.FullName `
        --no-build --no-restore --configuration Release `
        --collect:"XPlat Code Coverage" `
        --results-directory $outDir `
        --logger "trx;LogFileName=$projName.trx" `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
}
" "
Write-Info "## DOTNET TEST COMPLETED -------------------------------------------------------"
" "
Write-Info "## SONAR SCANNER CHECKING RESULTS ----------------------------------------------"
" "
## SONAR END --------------------------------------------------------------------------------
dotnet sonarscanner end /d:sonar.login="`sqp_a2947406f6a418abc08d5dfc3fac5dde1e1efc8c"
" "
Write-Info "## SONAR SCANNER ANALYSIS COMPLETED --------------------------------------------"
" "
Write-Info "================================================================================"
Write-Info "== PROJECT FOLDER => '$rootFolder' FINISHED"
Write-Info "================================================================================"
" "