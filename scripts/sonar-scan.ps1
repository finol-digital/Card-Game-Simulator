<#
.SYNOPSIS
    Runs the SonarCloud C# rule set (SonarAnalyzer.CSharp) locally via `dotnet build` and
    reports the issues that would fail the SonarCloud quality gate.

.DESCRIPTION
    The CI pipeline (.github/workflows/main.yml) runs a full SonarScanner analysis and a
    quality-gate check on every push. That feedback only arrives after a build in the cloud.

    This script reproduces the *rule set* locally: the same csharpsquid rules (e.g. S2219)
    run as Roslyn analyzers during `dotnet build`, wired in by Directory.Build.targets.
    It also mirrors the CI path exclusions (sonar.exclusions) so plugin/vendor code is
    ignored, and by default only reports issues in files changed on the current branch or
    working tree - matching the gate's "new code" (leak period) behaviour.

.PARAMETER All
    Report every Sonar warning in the whole solution, not just changed files.

.PARAMETER Hook
    Claude Code Stop-hook mode: read the hook JSON from stdin, avoid re-trigger loops, keep
    stdout limited to the hook JSON, and emit a `block` decision (surfacing the findings to
    the agent) when changed-file issues are found.

.EXAMPLE
    pwsh scripts/sonar-scan.ps1              # scan changed C# files
    pwsh scripts/sonar-scan.ps1 -All         # scan the entire solution
#>
param(
    [switch]$All,
    [switch]$Hook
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

# Keep in sync with .sonar/analyzers/.version and Directory.Build.targets.
$AnalyzerVersion = '10.28.0.143324'
$AnalyzerDir = Join-Path $repoRoot '.sonar/analyzers'
$AnalyzerDll = Join-Path $AnalyzerDir 'SonarAnalyzer.CSharp.dll'
$Solution    = Join-Path $repoRoot 'Card-Game-Simulator.sln'

# Paths excluded by the CI Sonar scan (sonar.exclusions in .github/workflows/main.yml).
$ExcludedPatterns = @('*/Assets/Plugins/*', '*/docs/*', '*.css', '*.scss', '*.sass')

# In hook mode stdout must contain ONLY the final JSON, so route progress to stderr.
function Note([string]$msg) {
    if ($Hook) { [Console]::Error.WriteLine($msg) } else { Write-Host $msg }
}

function Get-HookInput {
    if (-not $Hook) { return $null }
    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
    try { return $raw | ConvertFrom-Json } catch { return $null }
}

$hookInput = Get-HookInput
# Prevent infinite Stop-hook loops: if we already blocked once, let the session stop.
if ($Hook -and $hookInput -and $hookInput.stop_hook_active) { exit 0 }

function Confirm-Analyzer {
    if (Test-Path $AnalyzerDll) { return }
    Note "Fetching SonarAnalyzer.CSharp $AnalyzerVersion ..."
    New-Item -ItemType Directory -Force -Path $AnalyzerDir | Out-Null
    $tmp = Join-Path $AnalyzerDir 'sonar.nupkg'
    $url = "https://api.nuget.org/v3-flatcontainer/sonaranalyzer.csharp/$AnalyzerVersion/sonaranalyzer.csharp.$AnalyzerVersion.nupkg"
    Invoke-WebRequest -Uri $url -OutFile $tmp
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead($tmp)
    try {
        $entry = $zip.Entries | Where-Object { $_.FullName -eq 'analyzers/SonarAnalyzer.CSharp.dll' }
        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $AnalyzerDll, $true)
    } finally { $zip.Dispose() }
    Remove-Item $tmp -Force
    Set-Content -Path (Join-Path $AnalyzerDir '.version') -Value $AnalyzerVersion
}

# Build a map of repo-relative path -> set of changed (new-side) line numbers, mirroring
# SonarCloud's "new code" behaviour so we only flag issues on lines this branch touched.
# A $null value means "every line is new" (untracked file).
function Get-ChangedLines {
    $map = @{}
    $diffs = @()
    $diffs += , (git diff --unified=0 2>$null)
    $diffs += , (git diff --cached --unified=0 2>$null)
    $base = (git merge-base origin/main HEAD 2>$null)
    if (-not $base) { $base = (git merge-base main HEAD 2>$null) }
    if ($base) { $diffs += , (git diff --unified=0 "$base...HEAD" 2>$null) }

    $current = $null
    foreach ($diff in $diffs) {
        foreach ($ln in $diff) {
            if ($ln -match '^\+\+\+ b/(.*)$') {
                $current = ($matches[1] -replace '\\', '/')
                if (-not $map.ContainsKey($current)) { $map[$current] = New-Object System.Collections.Generic.HashSet[int] }
            }
            elseif ($ln -match '^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@') {
                if ($null -eq $current -or $null -eq $map[$current]) { continue }
                $start = [int]$matches[1]
                $count = if ($matches[2]) { [int]$matches[2] } else { 1 }
                for ($i = 0; $i -lt $count; $i++) { [void]$map[$current].Add($start + $i) }
            }
        }
    }
    (git ls-files --others --exclude-standard 2>$null) | ForEach-Object {
        if ($_ -like '*.cs') { $map[($_ -replace '\\', '/')] = $null }
    }
    return $map
}

function Test-Excluded([string]$path) {
    $p = $path -replace '\\', '/'
    foreach ($pat in $ExcludedPatterns) { if ($p -like $pat) { return $true } }
    return $false
}

# --- main -------------------------------------------------------------------

if (-not (Test-Path $Solution)) {
    Note "Sonar scan skipped: Card-Game-Simulator.sln not found. Generate it from Unity (Assets > Open C# Project, or RiderScriptEditor.SyncSolution)."
    exit 0
}

$changedLines = Get-ChangedLines
$changedCs = @($changedLines.Keys | Where-Object { $_ -like '*.cs' -and -not (Test-Excluded $_) })

if (-not $All -and $changedCs.Count -eq 0) {
    Note "No changed C# files to scan."
    exit 0
}

Confirm-Analyzer

Note "Running local Sonar analysis (dotnet build)..."
$buildOutput = & dotnet build $Solution -v q -clp:NoSummary --tl:off 2>&1

$regex = [regex]'\((?<line>\d+),\d+\):\s+warning\s+(?<rule>S\d+):\s+(?<msg>.+?)\s+\['
$findings = @()
foreach ($line in $buildOutput) {
    $clean = ($line.ToString()) -replace "`e\[[0-9;]*m", ''   # strip ANSI colour codes
    $m = $regex.Match($clean)
    if (-not $m.Success) { continue }
    $file = ($clean -split '\(', 2)[0]
    if (Test-Excluded $file) { continue }
    $rel = ($file -replace [regex]::Escape($repoRoot), '').TrimStart('\', '/') -replace '\\', '/'
    $lineNo = [int]$m.Groups['line'].Value
    if (-not $All) {
        $key = $changedCs | Where-Object { $rel -like "*$_" } | Select-Object -First 1
        if (-not $key) { continue }
        $lines = $changedLines[$key]
        # $null => whole (untracked) file counts; otherwise require the exact line to be new.
        if ($null -ne $lines -and -not $lines.Contains($lineNo)) { continue }
    }
    $findings += [pscustomobject]@{
        File = $rel; Line = $lineNo; Rule = $m.Groups['rule'].Value; Message = $m.Groups['msg'].Value
    }
}
$findings = @($findings | Sort-Object File, Line, Rule -Unique)

if ($findings.Count -eq 0) {
    Note "No Sonar issues in scanned code."
    exit 0
}

$report = ($findings | ForEach-Object { "  {0}:{1}  {2}  {3}" -f $_.File, $_.Line, $_.Rule, $_.Message }) -join "`n"
$scope = if ($All) { 'the solution' } else { 'changed files' }
$summary = "Local Sonar found $($findings.Count) issue(s) in $scope that would fail the SonarCloud quality gate:`n$report"

if ($Hook) {
    $reason = "$summary`n`nFix these before finishing (they mirror the SonarCloud gate that must pass on merge to main), or explain why they are acceptable."
    [Console]::Out.WriteLine((@{ decision = 'block'; reason = $reason } | ConvertTo-Json -Compress))
    exit 0
}

Write-Host $summary
exit 1
