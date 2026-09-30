param(
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'

$potionRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $scriptsRoot = Split-Path $potionRoot -Parent
    $assetsRoot = Split-Path $scriptsRoot -Parent
    $ProjectRoot = Split-Path $assetsRoot -Parent
}

$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure([string]$Message) {
    $script:failures.Add($Message)
}

function Read-Text([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Add-Failure "Missing file: $Path"
        return ''
    }

    $text = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    return $text.Replace("`r`n", "`n")
}

function Assert-Match([string]$Text, [string]$Pattern, [string]$Message) {
    if ($Text -notmatch $Pattern) {
        Add-Failure $Message
    }
}

function Read-Guid([string]$MetaPath) {
    $text = Read-Text $MetaPath
    if ($text -match '(?m)^guid: ([0-9a-f]{32})$') {
        return $Matches[1]
    }

    Add-Failure "Invalid or missing GUID: $MetaPath"
    return ''
}

$singleRoot = Join-Path $potionRoot 'Resources/SinglePotionDefaults'
$combinationRoot = Join-Path $potionRoot 'Resources/CombinationDefaults'

$singleDefinitions = @(
    @{ Name = 'Fire'; Element = 1; Radius = '2'; Duration = '4'; Interval = '1'; Thirst = '-30' },
    @{ Name = 'Poison'; Element = 2; Radius = '2'; Duration = '5'; Interval = '1'; Thirst = '-30' },
    @{ Name = 'Ice'; Element = 3; Radius = '2'; Duration = '4'; Interval = '1'; Thirst = '-30' },
    @{ Name = 'Heal'; Element = 4; Radius = '2'; Duration = '4'; Interval = '1'; Thirst = '-40' },
    @{ Name = 'Lightning'; Element = 5; Radius = '2'; Duration = '2'; Interval = '0.5'; Thirst = '-30' },
    @{ Name = 'Vaporization'; Element = 6; Radius = '2.5'; Duration = '0'; Interval = '0'; Thirst = '30' }
)

$combinationDefinitions = @(
    @{ Name = 'VampiricFire'; Id = 1; A = 4; B = 1; Duration = '0'; Radius = '2'; Floor = 1; Inherit = 1; LifeSteal = '0.2' },
    @{ Name = 'VampiricPoison'; Id = 2; A = 4; B = 2; Duration = '0'; Radius = '2'; Floor = 1; Inherit = 2; LifeSteal = '0.2' },
    @{ Name = 'VampiricFrost'; Id = 3; A = 4; B = 3; Duration = '0'; Radius = '2'; Floor = 1; Inherit = 3; LifeSteal = '0.2' },
    @{ Name = 'VampiricLightning'; Id = 4; A = 4; B = 5; Duration = '0'; Radius = '2'; Floor = 1; Inherit = 5; LifeSteal = '0.2' },
    @{ Name = 'ToxicFlame'; Id = 6; A = 1; B = 2; Duration = '5'; Radius = '2'; Floor = 1; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'ThermalShock'; Id = 7; A = 1; B = 3; Duration = '4'; Radius = '2'; Floor = 1; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'BlazingCurrent'; Id = 8; A = 1; B = 5; Duration = '4'; Radius = '2'; Floor = 1; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'FireExplosion'; Id = 9; A = 1; B = 6; Duration = '0'; Radius = '2.5'; Floor = 0; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'FrostPoison'; Id = 10; A = 2; B = 3; Duration = '5'; Radius = '2'; Floor = 1; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'ToxicCurrent'; Id = 11; A = 2; B = 5; Duration = '5'; Radius = '2'; Floor = 1; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'ToxicSteam'; Id = 12; A = 2; B = 6; Duration = '0'; Radius = '2.5'; Floor = 0; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'FrozenCurrent'; Id = 13; A = 3; B = 5; Duration = '4'; Radius = '2'; Floor = 1; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'FrostExplosion'; Id = 14; A = 3; B = 6; Duration = '0'; Radius = '2.5'; Floor = 0; Inherit = 0; LifeSteal = '0' },
    @{ Name = 'Discharge'; Id = 15; A = 5; B = 6; Duration = '0'; Radius = '2.5'; Floor = 0; Inherit = 0; LifeSteal = '0' }
)

foreach ($definition in $singleDefinitions) {
    $path = Join-Path $singleRoot ($definition.Name + '.asset')
    $text = Read-Text $path
    Assert-Match $text "(?m)^  _element: $($definition.Element)$" "$($definition.Name): element mismatch"
    Assert-Match $text "(?m)^  _effectRadius: $([regex]::Escape($definition.Radius))$" "$($definition.Name): radius mismatch"
    Assert-Match $text "(?m)^  _floorDuration: $([regex]::Escape($definition.Duration))$" "$($definition.Name): floor duration mismatch"
    Assert-Match $text "(?m)^  _tickInterval: $([regex]::Escape($definition.Interval))$" "$($definition.Name): tick interval mismatch"
    Assert-Match $text "(?m)^  _thirstDelta: $([regex]::Escape($definition.Thirst))$" "$($definition.Name): thirst delta mismatch"
}

foreach ($definition in $combinationDefinitions) {
    $path = Join-Path $combinationRoot ($definition.Name + '.asset')
    $text = Read-Text $path
    Assert-Match $text "(?m)^  _combination: $($definition.Id)$" "$($definition.Name): combination id mismatch"
    Assert-Match $text "(?m)^  _elementA: $($definition.A)$" "$($definition.Name): element A mismatch"
    Assert-Match $text "(?m)^  _elementB: $($definition.B)$" "$($definition.Name): element B mismatch"
    Assert-Match $text "(?m)^  _floorDuration: $([regex]::Escape($definition.Duration))$" "$($definition.Name): duration mismatch"
    Assert-Match $text "(?m)^  _effectRadius: $([regex]::Escape($definition.Radius))$" "$($definition.Name): radius mismatch"
    Assert-Match $text "(?m)^  _createsFloor: $($definition.Floor)$" "$($definition.Name): floor mode mismatch"
    Assert-Match $text "(?m)^  _lifeStealRatio: $([regex]::Escape($definition.LifeSteal))$" "$($definition.Name): life steal mismatch"

    if ($definition.Inherit -ne 0) {
        Assert-Match $text "(?m)^  _inheritThrownEffectsFrom: $($definition.Inherit)$" "$($definition.Name): inherited element mismatch"
    } elseif ($text -match '(?m)^  _inheritThrownEffectsFrom: (?!0$)') {
        Add-Failure "$($definition.Name): inherited element must be None"
    }
}

$unexpectedPurifyingDefault = Join-Path $combinationRoot 'PurifyingLife.asset'
if (Test-Path -LiteralPath $unexpectedPurifyingDefault) {
    Add-Failure 'PurifyingLife must remain outside the new active combination defaults.'
}

$assetFiles = @(
    Get-ChildItem -LiteralPath $singleRoot -Filter '*.asset' -File
    Get-ChildItem -LiteralPath $combinationRoot -Filter '*.asset' -File
)
$guids = foreach ($asset in $assetFiles) { Read-Guid ($asset.FullName + '.meta') }
$validGuids = @($guids | Where-Object { $_ })
if (@($validGuids | Select-Object -Unique).Count -ne $validGuids.Count) {
    Add-Failure 'Potion internal default asset GUIDs are not unique.'
}

$externalPotionRoot = Join-Path $ProjectRoot 'Assets/07.Datas/PotionData'
$externalCombinationRoot = Join-Path $ProjectRoot 'Assets/07.Datas/PotionCombinationData'
$potionDatabase = Read-Text (Join-Path $externalPotionRoot 'PotionDatabase.asset')
$combinationDatabasePath = Join-Path $externalCombinationRoot 'PotionCombinationDatabase.asset'
$combinationDatabase = Read-Text $combinationDatabasePath

$externalPotionAssets = @(Get-ChildItem -LiteralPath $externalPotionRoot -Filter '*Potion.asset' -File)
foreach ($asset in $externalPotionAssets) {
    $guid = Read-Guid ($asset.FullName + '.meta')
    if ($guid -and $potionDatabase -notmatch [regex]::Escape("guid: $guid")) {
        Add-Failure "PotionDatabase does not reference $($asset.Name)."
    }
    $text = Read-Text $asset.FullName

}

$externalCombinationAssets = @(
    Get-ChildItem -LiteralPath $externalCombinationRoot -Filter '*.asset' -File |
        Where-Object { $_.Name -match '^\d{2}_.+\.asset$' }
)
foreach ($asset in $externalCombinationAssets) {
    $guid = Read-Guid ($asset.FullName + '.meta')
    $text = Read-Text $asset.FullName
    if ($asset.Name -eq '05_PurifyingLife.asset') {
        if ($combinationDatabase -match [regex]::Escape("guid: $guid")) {
            Add-Failure 'PurifyingLife must not be in the active database.'
        }
        Assert-Match $text '(?m)^  _effects: \[\]$' 'PurifyingLife legacy effects must be cleared.'
        continue
    }
    if ($guid -and $combinationDatabase -notmatch [regex]::Escape("guid: $guid")) {
        Add-Failure "PotionCombinationDatabase does not reference $($asset.Name)."
    }

}
if ([regex]::Matches($combinationDatabase, '(?m)^  - .*guid:').Count -ne 14) {
    Add-Failure 'Expected exactly 14 active combinations in the database.'
}

if ($externalPotionAssets.Count -ne 6) {
    Add-Failure "Expected 6 external potion assets, found $($externalPotionAssets.Count)."
}
if ($externalCombinationAssets.Count -ne 15) {
    Add-Failure "Expected 15 external combination assets, found $($externalCombinationAssets.Count)."
}

$combinationDatabaseGuid = Read-Guid ($combinationDatabasePath + '.meta')
$groundAreaPrefab = Read-Text (Join-Path $ProjectRoot 'Assets/03.Prefabs/PotionGroundArea.prefab')
Assert-Match $groundAreaPrefab 'Assembly-CSharp::PotionAreaEffect' 'PotionGroundArea prefab is missing PotionAreaEffect.'
Assert-Match $groundAreaPrefab 'Assembly-CSharp::PotionAreaCombiner' 'PotionGroundArea prefab is missing PotionAreaCombiner.'
Assert-Match $groundAreaPrefab 'Assembly-CSharp::PotionCombinedAreaShape' 'PotionGroundArea prefab is missing PotionCombinedAreaShape.'
if ($combinationDatabaseGuid) {
    Assert-Match $groundAreaPrefab ([regex]::Escape("guid: $combinationDatabaseGuid")) 'PotionGroundArea prefab is not connected to PotionCombinationDatabase.'
}

$playerPrefab = Read-Text (Join-Path $ProjectRoot 'Assets/03.Prefabs/Player/Player.prefab')
Assert-Match $playerPrefab 'Assembly-CSharp::PotionEffectReceiver' 'Player prefab is missing PotionEffectReceiver.'

# Data is editable per asset; external assets need not match the defaults.
foreach ($asset in @($assetFiles) + @($externalPotionAssets) + @($externalCombinationAssets)) {
    $text = Read-Text $asset.FullName
    if ($text -match '(?m)^  _(effectProfile|hasEffectProfile|flightSpeed|maxThrowDistance|areaWidth|areaDuration|areaEffectAmount|effectDuration|drinkEffectAmount|drinkThirstRecoveryAmount|combinationDuration|instantOnly):') {
        Add-Failure "$($asset.Name): obsolete serialized field remains"
    }
    $keys = [regex]::Matches($text, '(?m)^  (_\w+):') | ForEach-Object { $_.Groups[1].Value }
    if (@($keys | Select-Object -Unique).Count -ne @($keys).Count) {
        Add-Failure "$($asset.Name): duplicate serialized fields"
    }
    foreach ($key in @('_targetType', '_effectRadius', '_createsFloor', '_floorDuration', '_tickInterval', '_applyOnEnter', '_lifeStealRatio', '_effects', '_frost')) {
        Assert-Match $text ("(?m)^  " + $key + ':') "$($asset.Name): missing $key"
    }
    if ($text -match 'Assembly-CSharp::PotionCombinationData') {
        if ($text -match '_thirstDelta:') { Add-Failure "$($asset.Name): combination thirst remains" }
    } else {
        Assert-Match $text '(?m)^  _thirstDelta:' "$($asset.Name): missing drink thirst"
    }
}
if ($failures.Count -gt 0) {
    Write-Host "Potion asset validation FAILED ($($failures.Count))" -ForegroundColor Red
    foreach ($failure in $failures) { Write-Host " - $failure" -ForegroundColor Red }
    exit 1
}

Write-Host 'Potion asset validation PASS' -ForegroundColor Green
Write-Host " - Internal defaults: $($singleDefinitions.Count) single + $($combinationDefinitions.Count) active combinations"
Write-Host " - Unique internal asset GUIDs: $($validGuids.Count)"
Write-Host " - External databases: $($externalPotionAssets.Count) potions + $($externalCombinationAssets.Count) combinations"
Write-Host ' - Prefab entry points: ground area/database and player receiver connected'
Write-Host ' - Data assets store their effects directly; no nested profiles or legacy fields'
Write-Host ' - PurifyingLife retained as inactive asset; combination thirst and potion flight settings removed'
