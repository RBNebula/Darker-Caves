namespace DarkCaves.Core;

internal struct LightStripStats
{
    public int Matched;
    public int Disabled;
    public int DriverBehavioursDisabled;
}

internal struct RendererStripStats
{
    public int LightmapCleared;
    public int LightProbeUsageDisabled;
    public int PlaneRenderersDisabled;
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
    public int TerrainLightmapsCleared;
    public int TerrainRealtimeLightmapsCleared;
    public int TerrainSplatPropertyBlocksApplied;
    public int TerrainFoliageSuppressed;
    public int TerrainDataGrassSuppressed;
}

