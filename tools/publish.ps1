param([string]$Configuration='Release')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;Set-Location $root
$version=([xml](Get-Content (Join-Path $root 'Directory.Build.props'))).Project.PropertyGroup.Version;if([string]::IsNullOrWhiteSpace($version)){$version='0.1.0'}
dotnet test ViewportCap.sln -c $Configuration --filter "Category!=Integration"
if($LASTEXITCODE-ne 0){throw "Tests failed (exit $LASTEXITCODE)."}
powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'fetch-ffmpeg.ps1')
if($LASTEXITCODE-ne 0){throw "FFmpeg fetch failed (exit $LASTEXITCODE)."}
$publishDir=Join-Path $root 'dist\publish';$zipPath=Join-Path $root "dist\viewport-cap-$version-win-x64.zip";if(Test-Path $publishDir){Remove-Item $publishDir -Recurse -Force};if(Test-Path $zipPath){Remove-Item $zipPath -Force}
dotnet publish src/ViewportCap.App/ViewportCap.App.csproj -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false -p:PublishReadyToRun=true -p:SatelliteResourceLanguages=en -o $publishDir
if($LASTEXITCODE-ne 0){throw "Publish failed (exit $LASTEXITCODE)."}
Remove-Item (Join-Path $publishDir '*.pdb') -Force
Copy-Item (Join-Path $PSScriptRoot 'ffmpeg\ffmpeg.exe') (Join-Path $publishDir 'ffmpeg.exe') -Force;Copy-Item (Join-Path $root 'LICENSE') $publishDir -Force;Copy-Item (Join-Path $root 'THIRD-PARTY-NOTICES.md') $publishDir -Force;Copy-Item (Join-Path $root 'README.md') $publishDir -Force
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -Force;Write-Host "Published $zipPath"
