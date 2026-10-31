param(
    [string]$SourceDirectory = (Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) 'build-check\cemu-fork'),
    [switch]$InitializeSubmodules,
    [switch]$UpdatePreparedFork
)
$ErrorActionPreference = 'Stop'
$project = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$source = [IO.Path]::GetFullPath($SourceDirectory)
$baseCommit = 'a6fb0a48eb437a8a41c13b782ac8ae0433bf8f98'
$utf8 = New-Object Text.UTF8Encoding($false)
if ($source -eq $project -or -not $source.StartsWith($project.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'La directory del fork deve essere una sottocartella separata di questo progetto.'
}
if (-not (Test-Path -LiteralPath $source)) {
    & git clone --depth 1 --branch v2.6 https://github.com/cemu-project/Cemu.git $source
    if ($LASTEXITCODE -ne 0) { throw 'Download del sorgente Cemu fallito.' }
}
if (-not (Test-Path -LiteralPath (Join-Path $source '.git'))) { throw 'La destinazione esistente non e un checkout Git di Cemu.' }
$head = (& git -C $source rev-parse HEAD | Out-String).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Impossibile identificare il checkout Cemu.' }
if ($head -ne $baseCommit) {
    if (-not $UpdatePreparedFork -or -not (Test-Path -LiteralPath (Join-Path $source '.wiiu-achievements-stage.json'))) {
        throw ('Richiesto il checkout esatto Cemu 2.6 (' + $baseCommit + '); trovato ' + $head + '. Per aggiornare un fork gia preparato usare -UpdatePreparedFork. Nessun reset automatico eseguito.')
    }
    & git -C $source merge-base --is-ancestor $baseCommit HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Il fork non discende dalla base Cemu 2.6 richiesta.' }
}
$patch = Join-Path $PSScriptRoot 'cemu-2.6.patch'
if (-not (Test-Path -LiteralPath $patch)) { throw 'Patch di integrazione Cemu non trovata.' }

# Fail before writing if the hooks differ from both the pristine and patched state.
function Test-AppliedPatch {
    # A failed reverse-check is expected on a pristine checkout. Windows PowerShell
    # otherwise turns git's stderr into a terminating NativeCommandError here.
    $ErrorActionPreference = 'Continue'
    & git -C $source apply --reverse --check -- $patch 2>$null
    return $LASTEXITCODE -eq 0
}
$alreadyApplied = Test-AppliedPatch
if (-not $alreadyApplied) {
    & git -C $source apply --check -- $patch
    if ($LASTEXITCODE -ne 0) { throw 'La patch confligge con modifiche locali. Nessun file e stato sovrascritto.' }
}

# The overlay is an explicit source/artwork allowlist, never a recursive project copy.
$files = @{}
function Add-StageFile([string]$From, [string]$Relative) {
    if (-not (Test-Path -LiteralPath $From -PathType Leaf)) { throw ('Sorgente necessario assente: ' + $From) }
    $files[$Relative.Replace('\', '/')] = $From
}
$nativeRoot = Join-Path $PSScriptRoot 'native'
Get-ChildItem -LiteralPath $nativeRoot -File -Recurse | Where-Object { $_.Extension -in @('.cpp', '.h', '.hpp', '.txt') } | ForEach-Object {
    Add-StageFile $_.FullName ('src/WiiUAchievements/' + $_.FullName.Substring($nativeRoot.Length + 1))
}
foreach ($name in @('build.ps1', 'achievements.xml', 'achievements-story.xml', 'achievements-planned.xml', 'achievement-presentation.xml', 'HANDOFF.md')) {
    Add-StageFile (Join-Path $project $name) ('wiiu-achievements-managed/' + $name)
}
Get-ChildItem -LiteralPath (Join-Path $project 'src') -File -Filter '*.cs' | ForEach-Object {
    Add-StageFile $_.FullName ('wiiu-achievements-managed/src/' + $_.Name)
}
Get-ChildItem -LiteralPath (Join-Path $project 'tests') -File -Filter '*.cs' | ForEach-Object {
    Add-StageFile $_.FullName ('wiiu-achievements-managed/tests/' + $_.Name)
}
foreach ($name in @('run.ps1', 'run-native.ps1')) {
    $testScript = Join-Path $project ('tests/' + $name)
    if (Test-Path -LiteralPath $testScript) { Add-StageFile $testScript ('wiiu-achievements-managed/tests/' + $name) }
}
$assetRoot = Join-Path $project 'assets'
Get-ChildItem -LiteralPath $assetRoot -File -Recurse | Where-Object {
    $_.Extension.ToLowerInvariant() -in @('.png', '.mp4', '.ttf', '.ico') -or $_.FullName -eq (Join-Path $assetRoot 'fonts\OFL.txt')
} | ForEach-Object { Add-StageFile $_.FullName ('wiiu-achievements-managed/assets/' + $_.FullName.Substring($assetRoot.Length + 1)) }
foreach ($name in @('ARTWORK-SOURCES.md', 'UI.md', 'MEMORY-MAP.md', 'PLAYTHROUGH.md', 'VALIDATION.md', 'COLLABORATION.md', 'ARCHITECTURE.md', 'CEMU-INTEGRATION.md')) {
    Add-StageFile (Join-Path $project ('docs/' + $name)) ('wiiu-achievements-managed/docs/' + $name)
}
Add-StageFile (Join-Path $PSScriptRoot 'workflows/wiiu-achievements.yml') '.github/workflows/wiiu-achievements.yml'
Add-StageFile (Join-Path $PSScriptRoot 'build.ps1') 'wiiu-achievements-managed/build-cemu.ps1'
Add-StageFile (Join-Path $PSScriptRoot 'FORK.md') 'WIIU-ACHIEVEMENTS.md'
$nativeTestsRoot = Join-Path $PSScriptRoot 'native-tests'
Get-ChildItem -LiteralPath $nativeTestsRoot -File -Recurse | Where-Object { $_.Extension -in @('.cpp', '.h', '.ps1') } | ForEach-Object {
    Add-StageFile $_.FullName ('wiiu-achievements-managed/native-tests/' + $_.FullName.Substring($nativeTestsRoot.Length + 1))
}

$manifestPath = Join-Path $source '.wiiu-achievements-stage.json'
$previous = @{}
if (Test-Path -LiteralPath $manifestPath) {
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.baseCommit -ne $baseCommit) { throw 'Manifesto locale del fork incompatibile.' }
    foreach ($entry in $manifest.files) { $previous[$entry.path] = $entry.sha256 }
}
$entries = @()
foreach ($relative in ($files.Keys | Sort-Object)) {
    $destination = Join-Path $source $relative
    $hash = (Get-FileHash -LiteralPath $files[$relative] -Algorithm SHA256).Hash
    if (Test-Path -LiteralPath $destination) {
        $existing = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
        if ($existing -ne $hash -and (-not $previous.ContainsKey($relative) -or $existing -ne $previous[$relative])) {
            throw ('Modifica locale non riconosciuta: ' + $relative + '. Nessun overlay e stato scritto.')
        }
    }
    $entries += [pscustomobject]@{ path = $relative; sha256 = $hash }
}
if (-not $alreadyApplied) {
    & git -C $source apply -- $patch
    if ($LASTEXITCODE -ne 0) { throw 'Applicazione patch fallita.' }
}
foreach ($relative in ($files.Keys | Sort-Object)) {
    $destination = Join-Path $source $relative
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $files[$relative] -Destination $destination -Force
}
[IO.File]::WriteAllText($manifestPath, (@{ baseCommit = $baseCommit; files = @($entries) } | ConvertTo-Json -Depth 4), $utf8)
if ($InitializeSubmodules) {
    & git -C $source submodule update --init --recursive
    if ($LASTEXITCODE -ne 0) { throw 'Inizializzazione delle dipendenze Cemu fallita.' }
}
Write-Output ('Fork locale preparato: ' + $source)
Write-Output ('Base Cemu 2.6: ' + $baseCommit + '; overlay controllato: ' + $entries.Count + ' file.')
Write-Output 'Sorgenti, regole e risorse soltanto: nessun dato personale o file di gioco copiato.'
