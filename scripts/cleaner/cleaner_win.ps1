# Deletes build output, test results and editor/OS leftovers (Windows / PowerShell 7). The Linux/macOS twin is cleaner_mac.sh; both do
# the same steps and print the same messages. See README.md in this folder.
#
# Usage: ./scripts/cleaner/cleaner_win.ps1 [-DryRun]
#   -DryRun   only list what would be deleted
param([switch]$DryRun)
$ErrorActionPreference = "Stop"

function Write-Info
{
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

$rootFolder = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path

# What is considered garbage. Names, matched anywhere under the repository (never inside .git).
$directories = @("bin", "obj", "TestResults", ".sonarqube", ".vs")
$files = @(".DS_Store", "Thumbs.db", "*.user", "*.suo", "*.trx", "coverage.cobertura.xml", "coverage.opencover.xml")

Set-Location $rootFolder
$gitFolder = Join-Path $rootFolder ".git"

" "
Write-Info "================================================================================"
Write-Info "== PROJECT FOLDER => '$rootFolder' START"
Write-Info "================================================================================"
" "
Write-Info "## SEARCHING -------------------------------------------------------------------"
$found = New-Object System.Collections.Generic.List[string]
$stack = New-Object System.Collections.Generic.Stack[string]
$stack.Push($rootFolder)
while ($stack.Count -gt 0)
{
    $current = $stack.Pop()
    foreach ($item in Get-ChildItem -LiteralPath $current -Force)
    {
        if ($item.FullName -eq $gitFolder)
        {
            continue
        }
        if ($item.PSIsContainer)
        {
            if ($directories -contains $item.Name)
            {
                $found.Add($item.FullName)   # deleted as a whole; its content is not searched
            }
            else
            {
                $stack.Push($item.FullName)
            }
        }
        elseif ($files | Where-Object { $item.Name -like $_ })
        {
            $found.Add($item.FullName)
        }
    }
}

$selected = New-Object System.Collections.Generic.List[string]
$kept = 0
foreach ($path in ($found | Sort-Object))
{
    $relative = [IO.Path]::GetRelativePath($rootFolder, $path).Replace("\", "/")
    # never delete something that is committed
    $tracked = git ls-files -- $relative 2>$null | Select-Object -First 1
    if ($tracked)
    {
        Write-Host "Keeping './$relative' (it contains tracked files)"
        $kept++
    }
    else
    {
        $selected.Add($path)
    }
}
Write-Host "$($selected.Count) item(s) to delete, $kept kept"
Write-Info "## SEARCHING COMPLETED ---------------------------------------------------------"
" "

if ($DryRun)
{
    Write-Info "## DRY RUN: NOTHING IS DELETED -------------------------------------------------"
    foreach ($path in $selected)
    {
        Write-Host "would delete ./$([IO.Path]::GetRelativePath($rootFolder, $path).Replace('\', '/'))"
    }
}
else
{
    Write-Info "## DELETING --------------------------------------------------------------------"
    foreach ($path in $selected)
    {
        Remove-Item -LiteralPath $path -Recurse -Force
        Write-Host "deleted ./$([IO.Path]::GetRelativePath($rootFolder, $path).Replace('\', '/'))"
    }
    Write-Info "## DELETING COMPLETED ----------------------------------------------------------"
}
" "
Write-Info "================================================================================"
Write-Info "== PROJECT FOLDER => '$rootFolder' FINISHED"
Write-Info "================================================================================"
" "
