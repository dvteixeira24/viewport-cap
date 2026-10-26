$ErrorActionPreference = 'Stop'
$publishScript = Join-Path $PSScriptRoot '..\publish.ps1'
$originalLocation = Get-Location
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('opencode\viewport-cap-publish-tests-' + [guid]::NewGuid().ToString('N'))

# Stub only the external commands; run the real publishing script in an isolated checkout.
function dotnet {
    $global:PublishTestCalls.Add("dotnet $($args[0])")
    $global:LASTEXITCODE = 0
    if ($args[0] -eq $global:PublishTestFailure) { $global:LASTEXITCODE = 7; return }
    if ($args[0] -eq 'publish') {
        $destination = $args[[Array]::IndexOf($args, '-o') + 1]
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
        Set-Content (Join-Path $destination 'app.pdb') 'symbols'
        Set-Content (Join-Path $destination 'viewport-cap.exe') 'app'
    }
}
function powershell {
    $global:PublishTestCalls.Add('fetch')
    $global:LASTEXITCODE = 0
    if ($global:PublishTestFailure -eq 'fetch') { $global:LASTEXITCODE = 8; return }
    $ffmpeg = Join-Path (Get-Location) 'tools\ffmpeg'
    New-Item -ItemType Directory -Path $ffmpeg -Force | Out-Null
    Set-Content (Join-Path $ffmpeg 'ffmpeg.exe') 'binary'
}

try {
    foreach ($failure in @('test', 'fetch', 'publish', 'none')) {
        $root = Join-Path $scratch $failure
        $tools = Join-Path $root 'tools'
        New-Item -ItemType Directory -Path $tools -Force | Out-Null
        Copy-Item $publishScript (Join-Path $tools 'publish.ps1')
        Set-Content (Join-Path $root 'Directory.Build.props') '<Project><PropertyGroup><Version>0.0.0-test</Version></PropertyGroup></Project>'
        foreach ($name in @('LICENSE', 'README.md', 'THIRD-PARTY-NOTICES.md')) { Set-Content (Join-Path $root $name) 'fixture' }
        $global:PublishTestFailure = $failure
        $global:PublishTestCalls = New-Object 'System.Collections.Generic.List[string]'
        $failed = $false
        try { & (Join-Path $tools 'publish.ps1') } catch { $failed = $true }
        if ($failure -eq 'none') {
            if ($failed -or !(Test-Path (Join-Path $root 'dist\viewport-cap-0.0.0-test-win-x64.zip'))) { throw 'Successful commands did not produce the archive.' }
        } else {
            if (!$failed) { throw "Publishing continued after $failure failed." }
            $expectedCalls = switch ($failure) { 'test' { 1 }; 'fetch' { 2 }; 'publish' { 3 } }
            if ($global:PublishTestCalls.Count -ne $expectedCalls) { throw "Commands ran after $failure failed: $global:PublishTestCalls" }
            if (Test-Path (Join-Path $root 'dist\viewport-cap-0.0.0-test-win-x64.zip')) { throw 'A failed publish produced an archive.' }
        }
        Write-Host "PASS: $failure"
    }
} finally {
    Set-Location $originalLocation
    if (Test-Path $scratch) { Remove-Item $scratch -Recurse -Force }
    Remove-Variable PublishTestFailure, PublishTestCalls -Scope Global -ErrorAction SilentlyContinue
}
