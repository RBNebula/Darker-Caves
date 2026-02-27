# Dark Caves

A BepInEx 5 mod for MineMogul that removes cave/mineshaft baked lighting and related brightening effects to keep underground areas consistently dark and immersive.

## Current Version

`0.8.1`

## What It Does

- Strips scene light sources using fixed, tuned defaults for this mod.
- Clears baked lightmaps/light probes and re-applies clearing during scans.
- Disables reflection probes and post-processing that can re-brighten scenes.
- Removes terrain/foliage visual artifacts that appear too bright in dark caves.
- Preserves lantern/player light groups using keyword-based keep rules.
- Supports one-time-per-save object removals via SavableObject IDs.

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

`com.main.darkcaves`

Config file:

`<MineMogul>\BepInEx\config\com.main.darkcaves.cfg`

Only one setting is user-configurable:

- `General.Enabled = true/false`

All other behavior is hardcoded to this final version's defaults.

Save-scoped state file:

`<MineMogul>\BepInEx\config\DarkCaves.saveScopedRemoval.state`

## Repository Notes

- `bin/` and `obj/` are excluded from source control.
- This repository does not include proprietary game binaries from `Libs`.
