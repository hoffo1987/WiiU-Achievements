param([string]$OutputDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'Compilatore .NET Framework a 64 bit non trovato.' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
# Nunito (SIL OFL 1.1) is embedded so the EXE needs no installed fonts.
$fonts = Join-Path $PSScriptRoot 'assets\fonts'
$resources = @(@('Nunito-Regular.ttf', 'Nunito-Bold.ttf', 'Nunito-ExtraBold.ttf') | ForEach-Object {
    $font = Join-Path $fonts $_
    if (-not (Test-Path -LiteralPath $font)) { throw ('Font mancante: ' + $font) }
    '/resource:' + $font + ',WiiUAchievements.Fonts.' + $_
})
# The project owner's brand is shared by every game; game logos stay per-game.
$brand = Join-Path $PSScriptRoot 'assets\brand'
$resources += @(Get-ChildItem -LiteralPath $brand -Filter '*.png' -File | Sort-Object Name | ForEach-Object {
    '/resource:' + $_.FullName + ',WiiUAchievements.Brand.' + $_.Name
})
$appIcon = Join-Path $brand 'app.ico'
# Per-game artwork (owner icons, rarity frames, sky, banner video) becomes WiiUAchievements.Games.<game>.<path>.
$games = Join-Path $PSScriptRoot 'assets\games'
if (Test-Path -LiteralPath $games) {
    $resources += @(Get-ChildItem -LiteralPath $games -File -Recurse | Where-Object { @('.png', '.mp4') -contains $_.Extension.ToLowerInvariant() } | Sort-Object FullName | ForEach-Object {
        '/resource:' + $_.FullName + ',WiiUAchievements.Games.' + $_.FullName.Substring($games.Length + 1).Replace('\', '.')
    })
}
& $compiler /nologo /langversion:5 /target:winexe /platform:x64 /optimize+ /utf8output /r:System.Xml.Linq.dll /r:System.Web.Extensions.dll /r:System.Security.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Accessibility.dll "/win32icon:$appIcon" $resources "/out:$output\WiiUAchievements.exe" $sources
if ($LASTEXITCODE -ne 0) { throw 'Compilazione applicazione fallita.' }
if ($output.TrimEnd('\') -ne $PSScriptRoot.TrimEnd('\')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'achievements.xml') -Destination $output -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'achievements-planned.xml') -Destination $output -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'achievements-story.xml') -Destination $output -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'achievement-presentation.xml') -Destination $output -Force
}
Write-Output ('Applicazione compilata: ' + (Join-Path $output 'WiiUAchievements.exe'))
