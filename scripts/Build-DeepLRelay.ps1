param([Parameter(Mandatory = $true)][string]$PublishDir)
$ErrorActionPreference = "Stop"
$PublishDir = [IO.Path]::GetFullPath($PublishDir)
New-Item -ItemType Directory -Force -Path $PublishDir | Out-Null
$source = Join-Path $PSScriptRoot "../tools/deepl-relay"
$previous = @{}
foreach ($name in @("GOOS", "GOARCH", "CGO_ENABLED")) { $previous[$name] = [Environment]::GetEnvironmentVariable($name) }
Push-Location $source
try {
    $env:GOARCH = "amd64"
    $env:CGO_ENABLED = "0"
    foreach ($target in @(@("windows", "deepl-relay.exe"), @("linux", "deepl-relay"))) {
        $env:GOOS = $target[0]
        & go build -trimpath -ldflags "-s -w" -o (Join-Path $PublishDir $target[1]) .
        if ($LASTEXITCODE -ne 0) { throw "Building DeepL relay for $($target[0]) failed." }
    }
    # Carry notices alongside the distributed native binaries.
    $notices = [Collections.Generic.List[string]]::new()
    $goRoot = & go env GOROOT
    $goLicense = @((Join-Path $goRoot "LICENSE"), "/usr/share/licenses/go/LICENSE") | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $goLicense) { throw "Go standard-library license not found." }
    $notices.Add("Go standard library`n" + [IO.File]::ReadAllText($goLicense))
    $modules = & go list -m -f '{{.Path}}|{{.Dir}}' all
    if ($LASTEXITCODE -ne 0) { throw "Listing relay dependency licenses failed." }
    foreach ($module in $modules) {
        $parts = $module -split '\|', 2
        if ($parts.Length -lt 2 -or -not $parts[1]) { continue }
        if ($parts[0] -eq "github.com/NightlyRevenger/TataruHelper/tools/deepl-relay") { continue }
        $license = @("LICENSE", "LICENSE.txt", "LICENSE.md") | ForEach-Object { Join-Path $parts[1] $_ } | Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $license) { throw "Dependency license missing: $($parts[0])" }
        $notices.Add($parts[0] + "`n" + [IO.File]::ReadAllText($license))
    }
    [IO.File]::WriteAllText((Join-Path $PublishDir "THIRD-PARTY-NOTICES.deepl-relay.txt"), ($notices -join "`n`n----------------`n`n"))
}
finally {
    Pop-Location
    foreach ($name in $previous.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
}
