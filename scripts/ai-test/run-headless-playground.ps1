# Starts the demo site in the AiTest environment and the headless playground (src/SproutForms.Client/example) next to it,
# then opens the playground. Stop both with Ctrl+C.
param(
    # Delete the AiTest database, mail and uploads first, for a clean install
    [switch]$Reset,

    # Don't open the playground in the default browser
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'
$root = Join-Path $PSScriptRoot '..\..' | Resolve-Path
$example = Join-Path $root 'src\SproutForms.Client\example'

$siteArgs = @('-NoProfile', '-File', (Join-Path $PSScriptRoot 'run-site.ps1'))
if ($Reset) { $siteArgs += '-Reset' }
$site = Start-Process pwsh -ArgumentList $siteArgs -PassThru -NoNewWindow

try {
    Write-Host 'Waiting for the site on http://localhost:5970 ...'
    $deadline = (Get-Date).AddMinutes(5)
    while ($true) {
        try {
            Invoke-WebRequest 'http://localhost:5970/umbraco/sproutforms/delivery/api/v1/definitions/aiTestMultiPage' -TimeoutSec 5 | Out-Null
            break
        } catch {
            if ($site.HasExited) { throw 'The site stopped; see its output above.' }
            if ((Get-Date) -gt $deadline) { throw 'The site did not answer within 5 minutes.' }
            Start-Sleep -Seconds 2
        }
    }

    if (-not (Test-Path (Join-Path $example 'node_modules'))) {
        npm --prefix $example install
    }
    if (-not $NoBrowser) { Start-Process 'http://localhost:5173' }
    npm --prefix $example run dev
} finally {
    if (-not $site.HasExited) {
        # dotnet run starts the site as a child process, so stop the whole tree
        taskkill /PID $site.Id /T /F | Out-Null
    }
}
