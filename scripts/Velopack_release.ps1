param(
    [ValidateSet("stable", "prerelease")]
    [string]$Channel = "stable",
    [string]$Version = "",
    [string]$PackId = "TataruHelper",
    [string]$MainExe = "TataruHelper.exe",
    [string]$ProjectPath = "",
    [string]$PublishDir = "",
    [string]$OutputDir = "",
    [string]$RepoUrl = "https://github.com/NightlyRevenger/TataruHelper",
    [string]$RepoToken = "",
    [switch]$SkipPublish,
    [switch]$SkipDownloadLatest,

    # Ship whatever index is already in the publish folder instead of building
    # one. Building fetches the translation project in full: a few hundred
    # megabytes and some minutes.
    [switch]$SkipReferenceIndex,

    # A folder holding an unpacked xivrus export, when there is one at hand and
    # the download is not wanted. An index built that way carries no revision,
    # so the first update after install will fetch a fresh one.
    [string]$ReferenceIndexSource = ""
)

$ErrorActionPreference = "Stop"

$env:DOTNET_ROLL_FORWARD = "LatestMajor"

#region Helpers

function Assert-ExitCode {
    param([Parameter(Mandatory = $true)][string]$What)

    if ($LASTEXITCODE -ne 0) {
        throw "$What failed with exit code $LASTEXITCODE."
    }
}

function Resolve-ProjectVersion {
    param([Parameter(Mandatory = $true)][string]$AssemblyInfoPath)

    if (Test-Path $AssemblyInfoPath) {
        $content = Get-Content -Path $AssemblyInfoPath -Raw
        $match = [regex]::Match($content, 'Assembly(?:File)?Version\("(?<version>\d+\.\d+\.\d+)(?:\.\d+)?"\)')
        if ($match.Success) {
            return $match.Groups["version"].Value
        }
    }

    throw "Cannot resolve the version from '$AssemblyInfoPath'. Pass an explicit -Version (e.g. -Version 1.0.7) for this run."
}

function Resolve-Vpk {
    if (Get-Command vpk -ErrorAction SilentlyContinue) {
        return @{ Exe = "vpk"; Prefix = @() }
    }

    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        return @{ Exe = "dotnet"; Prefix = @("tool", "run", "vpk", "--") }
    }

    throw "Velopack CLI is not available. Install ``vpk`` (``dotnet tool install -g vpk --version 0.0.1298``) and retry."
}

function Invoke-Vpk {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$Capture
    )

    $all = @($script:Vpk.Prefix) + $Arguments

    $echoed = @($all)
    $tokenAt = [array]::IndexOf($echoed, "--token")
    if ($tokenAt -ge 0 -and $tokenAt + 1 -lt $echoed.Count) {
        $echoed[$tokenAt + 1] = "***"
    }
    Write-Host ("[Velopack] Command: " + ((@($script:Vpk.Exe) + $echoed) -join " "))

    if ($Capture) {
        $output = & $script:Vpk.Exe @all 2>&1
        return $output
    }

    & $script:Vpk.Exe @all
}

function Publish-Asset {
    param(
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$NewName
    )

    $match = Get-ChildItem -Path $OutputDir -Filter $Pattern -File -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $match) {
        Write-Warning "Build matching '$Pattern' was not found; $NewName was not written."
        return
    }

    $destination = Join-Path $OutputDir "$NewName$($match.Extension)"
    Move-Item $match.FullName $destination -Force
    Write-Host "[Velopack] Published as $(Split-Path $destination -Leaf)."
}

function Get-OutputFiles {
    param([string]$Filter = "*")

    return @(Get-ChildItem -Path $OutputDir -Filter $Filter -File -ErrorAction SilentlyContinue)
}

#endregion

#region Steps

function Invoke-AppPublish {
    Write-Host "[Velopack] Publishing app binaries..."
    dotnet publish $ProjectPath `
        -c Release `
        -r win-x64 `
        --self-contained true `
        /p:PublishSingleFile=false `
        /p:PublishReadyToRun=true `
        -o $PublishDir
    Assert-ExitCode "dotnet publish"
}

function Build-ReferenceIndex {
    Write-Host "[Velopack] Building the reference translation index..."

    $indexPath = Join-Path $PublishDir "Resources/ReferenceTranslations.db"
    $indexArgs = @("--build-reference-index", "--output", $indexPath)
    if ($ReferenceIndexSource) {
        $indexArgs += @("--source", $ReferenceIndexSource)
    }

    $indexLog = Join-Path $OutputDir "reference-index.log"
    $indexErrorLog = Join-Path $OutputDir "reference-index.err.log"

    $indexProcess = Start-Process -FilePath (Join-Path $PublishDir $MainExe) `
        -ArgumentList $indexArgs -Wait -PassThru -NoNewWindow `
        -RedirectStandardOutput $indexLog -RedirectStandardError $indexErrorLog

    Get-Content $indexLog -ErrorAction SilentlyContinue | Write-Host

    if ($indexProcess.ExitCode -ne 0) {
        Get-Content $indexErrorLog -ErrorAction SilentlyContinue | Write-Warning
        throw "Building the reference index failed with exit code $($indexProcess.ExitCode). See '$indexLog'."
    }

    if (-not (Test-Path $indexPath)) {
        throw "The reference index was reported as built but '$indexPath' is not there."
    }
}

function Get-DeltaBase {
    $downloadArgs = @(
        "download",
        "github",
        "--repoUrl", $RepoUrl,
        "--channel", $Channel,
        "--outputDir", $OutputDir
    )

    $token = if (-not [string]::IsNullOrWhiteSpace($RepoToken)) { $RepoToken } else { $env:GITHUB_TOKEN }
    if (-not [string]::IsNullOrWhiteSpace($token)) {
        $downloadArgs += @("--token", $token)
    }

    Write-Host "[Velopack] Downloading latest published release for delta base..."
    Invoke-Vpk -Arguments $downloadArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Unable to download latest published release (exit code $LASTEXITCODE). Delta package may not be generated."
    }
}

function Invoke-Pack {
    Write-Host "[Velopack] Packaging release..."

    $packOutput = Invoke-Vpk -Capture -Arguments @(
        "pack",
        "--packId", $PackId,
        "--packVersion", $Version,
        "--packDir", $PublishDir,
        "--mainExe", $MainExe,
        "--channel", $Channel,
        "--outputDir", $OutputDir,
        "--icon", $iconPath
    )
    $packOutput | ForEach-Object { Write-Host $_ }

    if ($LASTEXITCODE -eq 0) {
        return $false
    }

    if (($packOutput | Out-String) -match "(?i)there is a release in channel .*? which is equal or greater to the current version") {
        Write-Host "[Velopack] Release $Version already exists on channel $Channel; nothing to package this run."
        return $true
    }

    throw "Velopack CLI failed with exit code $LASTEXITCODE."
}

#endregion

#region Release

$scriptRoot = Split-Path -Path $PSCommandPath -Parent
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repoRoot "TataruHelper/TataruHelper.csproj"
}

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = Join-Path $repoRoot "artifacts/publish/$PackId/win-x64"
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "artifacts/velopack/$Channel"
}

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Resolve-ProjectVersion -AssemblyInfoPath (Join-Path $repoRoot "TataruHelper/Properties/AssemblyInfo.cs")
}
Write-Host "[Velopack] Resolved version: $Version"

$iconPath = Join-Path $repoRoot "TataruHelper/Resources/app_icon2.ico"

if (-not $SkipPublish) {
    Invoke-AppPublish
}

if (-not (Test-Path (Join-Path $PublishDir $MainExe))) {
    throw "Main executable '$MainExe' was not found in '$PublishDir'."
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

if (-not $SkipReferenceIndex) {
    Build-ReferenceIndex
}

$script:Vpk = Resolve-Vpk
Invoke-Vpk -Arguments @("--version")

$filesBeforeDownload = @(Get-OutputFiles | Select-Object -ExpandProperty FullName)
if (-not $SkipDownloadLatest) {
    Get-DeltaBase
}
$downloadedFiles = @(Get-OutputFiles | Where-Object { $filesBeforeDownload -notcontains $_.FullName })

$deltaBaseCount = @(Get-OutputFiles -Filter "*-full.nupkg").Count
$deltaCountBeforePack = @(Get-OutputFiles -Filter "*-delta.nupkg").Count

$releaseAlreadyPublished = Invoke-Pack

if (-not $releaseAlreadyPublished) {
    Publish-Asset -Pattern "$PackId-$Channel-Setup.*" -NewName "Setup"
    Publish-Asset -Pattern "$PackId-$Channel-Portable.*" -NewName "Portable"
}

Write-Host "[Velopack] Done."
Write-Host "[Velopack] Channel: $Channel"
Write-Host "[Velopack] Version: $Version"
Write-Host "[Velopack] PublishDir: $PublishDir"
Write-Host "[Velopack] OutputDir: $OutputDir"

if ($releaseAlreadyPublished) {
    Write-Host "[Velopack] Nothing new was packaged; release $Version already existed on channel $Channel."
}
elseif ($deltaBaseCount -eq 0) {
    Write-Host "[Velopack] No previous full package in outputDir; delta package is not expected for the first release in this environment."
}
elseif (@(Get-OutputFiles -Filter "*-delta.nupkg").Count -le $deltaCountBeforePack) {
    throw "Delta package was expected (previous full package exists in outputDir), but no new *-delta.nupkg was produced."
}
else {
    Write-Host "[Velopack] Delta package generated successfully."
}

$leftovers = @(if ($releaseAlreadyPublished) { $downloadedFiles } else { $downloadedFiles | Where-Object { $_.Extension -eq ".nupkg" } })
if ($leftovers.Count -gt 0) {
    $leftovers | Remove-Item -Force
}

#endregion
