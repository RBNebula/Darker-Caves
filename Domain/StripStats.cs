namespace DarkCaves.Domain;

internal struct LightStripStats
{
    public int Matched;
    public int Disabled;
    public int Destroyed;
    public int DriverBehavioursDisabled;
}

internal struct RendererStripStats
{
    public int LightmapCleared;
    public int LightProbeUsageDisabled;
    public int ReflectionProbeUsageDisabled;
}

internal struct EmissionStripStats
{
    public int RenderersModified;
    public int MaterialsModified;
}

internal struct DustStripStats
{
    public int ParticleSystemsStopped;
    public int ParticleRenderersDisabled;
    public int ParticleLightsDisabled;
    public int VisualEffectsDisabled;
}

internal struct TerrainStripStats
{
    public int TerrainMaterialsDarkened;
    public int TerrainRenderersDarkened;
    public int TerrainSlotsDarkened;
    public int TerrainLightmapsCleared;
    public int TerrainRealtimeLightmapsCleared;
    public int TerrainReflectionProbeUsageDisabled;
    public int TerrainSplatPropertyBlocksApplied;
    public int TerrainFoliageSuppressed;
    public int TerrainDataGrassSuppressed;
}
