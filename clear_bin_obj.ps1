#Clear screen
# clear

$rootFolder = (Get-Item -Path "./" -Verbose).FullName
" "
Write-Output "================================================================================"
Write-Output "== PROJECT FOLDER => '$rootFolder' START"
Write-Output "================================================================================"

#clear obj and bin folder
" "
Write-Output "## CLEAR ALL BIN & OBJ FOLDERS START -------------------------------------------"

Get-ChildItem .\ -include bin,obj -Recurse | foreach ($_) { remove-item $_.fullname -Force -Recurse }

Write-Output "## CLEAR ALL BIN & OBJ FOLDERS COMPLETED ---------------------------------------"


" "
Write-Output "================================================================================"
Write-Output "== PROJECT FOLDER => '$rootFolder' FINISHED"
Write-Output "================================================================================"