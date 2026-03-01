# DarkerCaves

A BepInEx 5 mod for MineMogul that removes cave/mineshaft baked lighting and related brightening effects to keep underground areas consistently dark and immersive.

## Current Version

`1.0.0`

## What It Does

- Strips scene light sources using fixed, tuned defaults for this mod.
- Clears baked lightmaps/light probes and re-applies clearing during scans.
- Disables reflection probes and post-processing that can re-brighten scenes.
- Removes emission from terrain materials/splat properties while avoiding broad global emission stripping.
- Targets dust motes via explicit built-in dustmote naming rules (no configurable CSV keyword list).
- Removes terrain/foliage visual artifacts that appear too bright in dark caves.
- Excludes all `ISaveLoadableObject` hierarchies (SavableObject-based items/placeables, including modded IDs) from darkening/stripping targeting.
- Excludes `InventoryItemPreview` hierarchies so inventory preview item colors remain unchanged.
- Excludes player hierarchies (`PlayerController`) so equipment-driven lights (e.g. mining helmet toggle) still function.

## Requirements

- MineMogul
- BepInEx 5
- .NET Framework 4.7.2 SDK (for local builds)

## Build

This project references game/modding libraries from a sibling `Libs` folder by default:

`..\Libs\`

Expected files include:

- `BepInEx.dll`
- `Assembly-CSharp.dll`
- `UnityEngine*.dll`
- `0Harmony.dll`

Build command:

```powershell
dotnet build -c Release
```

If your game libs are elsewhere, pass the path explicitly:

```powershell
dotnet build -c Release -p:GameLibsDir="D:\MineMogul\Libs"
```

Output DLL:

`bin\Release\net472\DarkCaves.dll`

## Deploy

Copy these files to your BepInEx plugins folder:

- `DarkCaves.dll`
- `DarkCaves.pdb` (optional but useful for debugging)

Typical path:

`<MineMogul>\BepInEx\plugins\`

## Config

Plugin GUID:

`com.darkcaves`

User-configurable settings:

- None. Behavior is fully hardcoded in this build.

All SavableObject-based items/placeables (including modded IDs) are excluded from darkening/stripping targeting.
Inventory previews (`InventoryItemPreview`) are also excluded from targeting.
Player hierarchies (`PlayerController`) are also excluded from targeting.

No hardcoded per-object ID removals or lantern/player name-based keep targeting are used.
Terrain handling now targets actual Unity `Terrain` components instead of terrain keyword/heuristic renderer matching.

All other behavior remains hardcoded to this version's defaults.

## Repository Notes

- `bin/` and `obj/` are excluded from source control.
- This repository does not include proprietary game binaries from `Libs`.
