param([string]$Destination=(Join-Path $PSScriptRoot 'ffmpeg'))
$ErrorActionPreference='Stop'
$url='https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2026-10-25-13-00/ffmpeg-n8.1.3-24-g92611900bb-win64-lgpl-8.1.zip'
$expectedHash='ed8b8dfe088ff978d657834cf96fe40d7dcbb011b37ff374005b66d5272448c9'
New-Item -ItemType Directory -Path $Destination -Force|Out-Null;$zip=Join-Path $Destination 'ffmpeg.zip';$extract=Join-Path $Destination 'extract'
if(Test-Path (Join-Path $Destination 'ffmpeg.exe')){Write-Host "ffmpeg.exe already present in $Destination";exit 0}
Invoke-WebRequest -Uri $url -OutFile $zip;$actualHash=(Get-FileHash -Algorithm SHA256 $zip).Hash.ToLowerInvariant();if($actualHash-ne$expectedHash){throw "FFmpeg checksum mismatch. Expected $expectedHash, got $actualHash."}
if(Test-Path $extract){Remove-Item $extract -Recurse -Force};Expand-Archive -Path $zip -DestinationPath $extract -Force;$binary=Get-ChildItem -Path $extract -Filter 'ffmpeg.exe' -Recurse|Select-Object -First 1;if(-not$binary){throw 'ffmpeg.exe was not found in the archive.'}
Copy-Item $binary.FullName (Join-Path $Destination 'ffmpeg.exe') -Force;Remove-Item $zip -Force;Remove-Item $extract -Recurse -Force;Write-Host "FFmpeg installed at $(Join-Path $Destination 'ffmpeg.exe')"
