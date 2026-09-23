# Removes everything the AiTest environment has stored, so the next start does a fresh unattended install.
# Stop the site first: SQLite keeps the database file locked while it runs.
$ErrorActionPreference = 'Stop'
$aiTestData = Join-Path $PSScriptRoot '..\..\src\SproutForms.Site\umbraco\Data\AiTest'

if (Test-Path $aiTestData) {
    Remove-Item -Recurse -Force $aiTestData
    Write-Host "Removed $((Resolve-Path (Split-Path $aiTestData)).Path)\AiTest"
}
else {
    Write-Host 'Nothing to reset'
}
