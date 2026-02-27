using BepInEx.Configuration;

namespace DarkCaves.Configuration;

internal sealed class DarkCavesConfig
{
    private readonly ConfigEntry<bool> _enabled;

    public DarkCavesConfig(ConfigFile config)
    {
        _enabled = config.Bind("General", "Enabled", true, "Master switch for the mod.");
    }

    public bool Enabled => _enabled.Value;
    public bool RunOncePerScene => true;
    public string IgnoreScenesCsv => "MainMenu";
    public float ScanDurationSeconds => 20f;
    public float ScanIntervalSeconds => 0.5f;
    public int HeavyPassIntervalScans => 8;

    public bool DestroyLightComponents => false;
    public bool TargetPointLightsOnly => false;
    public bool RemoveOnlyOrphanPointLights => false;
    public bool RemoveDustMoteLights => true;
    public string DustKeywordsCsv => "dustmote,dustmotes,dust_mote,dust_motes,dust mote,dust motes,particleslight,particle light,dustparticle,dust particle";
    public bool DisableLightDriverBehaviours => true;
    public bool PreserveLanternLights => true;
    public string LanternKeywordsCsv => "lantern,jackolantern,diamondolantern,newlantern";
    public bool PreservePlayerLights => true;
    public string PlayerKeywordsCsv => "mininghat,nightvision,flashlight,player";

    public bool ClearBakedLightmaps => true;
    public bool ClearLightProbes => true;
    public bool ReapplyBakedClearEachScan => true;
    public bool DisableReflectionProbes => true;
    public bool StripRendererLightmaps => true;
    public bool DisableRendererProbeUsage => true;
    public bool StripMaterialEmission => true;
    public bool DisableDustParticleSystems => true;
    public bool DisableProjectorLikeComponents => true;
    public bool DisableDustVisualEffects => true;
    public bool DisablePostProcessing => true;
    public bool DarkenTerrainMaterials => false;
    public bool TerrainAffectAllRenderers => false;
    public bool TerrainForceBlack => false;
    public bool TerrainForceBlackTextures => false;
    public bool TerrainResetPropertyBlocks => true;
    public bool SuppressTerrainFoliage => true;
    public bool TerrainUseHeuristicMatching => true;
    public float TerrainHeuristicMinSurfaceArea => 12f;
    public float TerrainHeuristicMaxThickness => 12f;
    public float TerrainColorMultiplier => 0f;
    public string TerrainKeywordsCsv => "terrain,ground,cavefloor,cave_ground,cave floor,floor,grass,dirt,mud,sand,soil";
    public bool ForceAmbientBlack => true;
    public bool ZeroReflectionIntensity => true;
    public bool DisableFog => true;
    public bool DisableSkybox => true;
    public bool EnableSaveScopedOneTimeRemoval => true;
    public string SaveScopedRemoveIdsCsv => "301,802,803";
    public string SaveScopedStateFileName => "DarkCaves.saveScopedRemoval.state";
}
