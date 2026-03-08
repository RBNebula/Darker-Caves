DARKER CAVES - 1.0.1
Makes cave and mineshaft scenes consistently darker in MineMogul.

:: REQUIREMENTS ::
- MineMogul (up to date)
- BepInEx 5

:: FEATURES ::
- Removes or disables cave/mineshaft light sources that over-brighten underground areas
- Clears baked lightmaps and light probes during stripping passes
- Disables reflection/post-processing effects that can brighten dark scenes
- Reduces bright terrain/emission visuals in underground spaces
- Removes bright dust mote style clutter effects
- Runs a darkness pass on scene load and after save-load events
- Excludes savable/placeable object hierarchies from stripping targeting
- Excludes inventory preview objects from stripping targeting
- Excludes player hierarchy from stripping targeting
- Fixed defaults (no user config required)

:: INSTALL ::
1. Copy `DarkCaves.dll` to:
   `MineMogul\BepInEx\plugins\`
2. (Optional) Copy `DarkCaves.pdb` for debugging logs/symbols.
3. Launch the game.

:: GENERAL OTHER STUFF ::
- Lighting/visual immersion mod only
- No save conversion required
- Designed to keep underground exploration moodier and less washed out

:: THIS MOD DOES NOT ADD LOOT OR ITEMS ::
This mod does not add items, blocks, stats, or progression bonuses.

:: KNOWN ISSUES ::
- None currently known.

:: CREDITS ::
- Made by RBN
