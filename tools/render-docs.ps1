param([string]$OutputDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\screenshots'))
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$stage = Join-Path ([IO.Path]::GetTempPath()) ('WiiUAchievements-docs-' + [guid]::NewGuid().ToString('N'))
& (Join-Path $project 'build.ps1') -OutputDirectory $stage
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$application = Join-Path $stage 'WiiUAchievements.exe'
$renderer = Join-Path $stage 'RenderDocumentation.exe'
& $compiler /nologo /langversion:5 /target:exe /platform:x64 /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:Accessibility.dll "/r:$application" "/out:$renderer" (Join-Path $PSScriptRoot 'RenderDocumentation.cs')
if ($LASTEXITCODE -ne 0) { throw 'Documentation renderer compilation failed.' }
& $renderer ([IO.Path]::GetFullPath($OutputDirectory)) (Join-Path $stage 'synthetic-data')
if ($LASTEXITCODE -ne 0) { throw 'Documentation rendering failed.' }
Write-Output ('Synthetic documentation images updated: ' + $OutputDirectory)
