#Clear screen
# clear

$rootFolder = (Get-Item -Path "./" -Verbose).FullName
" "
Write-Output "================================================================================"
Write-Output "== PROJECT FOLDER => '$rootFolder' START"
Write-Output "================================================================================"
" "

Write-Output "## SONAR SCANNER INSTALL -------------------------------------------------------"
$x = dotnet tool list -g | grep -c dotnet-sonarscanner
if(!($x -eq 1)){
    dotnet tool install --global dotnet-sonarscanner
}
else{
    write-host("dotnet-sonarscanner already installed")
}
Write-Output "## SONAR SCANNER INSTALL COMPLETED ---------------------------------------------"
" "

Write-Output "## SONAR SCANNER ANALYSIS ------------------------------------------------------"
dotnet sonarscanner begin /k:"hhs-service-content" /d:sonar.host.url="http://localhost:8031"  /d:sonar.login="sqp_55ab483b5d93eccddf8b6ddfb31fc9a550bcb250"
dotnet restore --force --verbosity minimal
dotnet build --no-restore --no-incremental --verbosity minimal -c release
dotnet sonarscanner end /d:sonar.login="sqp_55ab483b5d93eccddf8b6ddfb31fc9a550bcb250"
Write-Output "## SONAR SCANNER ANALYSIS COMPLETED --------------------------------------------"

" "
Write-Output "================================================================================"
Write-Output "== PROJECT FOLDER => '$rootFolder' FINISHED"
Write-Output "================================================================================"