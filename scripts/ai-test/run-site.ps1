# Starts SproutForms.Site in the AiTest environment (throwaway SQLite database, emails to a pickup folder).
# Used by .claude/launch.json; see .claude/skills/verify-in-site/SKILL.md.
param(
    # Delete the AiTest database, mail and temp data first, for a clean unattended install
    [switch]$Reset
)

$ErrorActionPreference = 'Stop'
$siteDir = Join-Path $PSScriptRoot '..\..\src\SproutForms.Site' | Resolve-Path

if ($Reset) {
    & (Join-Path $PSScriptRoot 'reset.ps1')
}

# SQLite and the mail pickup folder don't create their own directories
New-Item -ItemType Directory -Force -Path (Join-Path $siteDir 'umbraco\Data\AiTest\Mail') | Out-Null

Set-Location $siteDir
dotnet run --launch-profile AiTest
