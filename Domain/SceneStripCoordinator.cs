using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using DarkCaves.Configuration;
using DarkCaves.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DarkCaves.Domain;

internal sealed class SceneStripCoordinator
{
    private readonly MonoBehaviour _host;
    private readonly ManualLogSource _logger;
    private readonly DarkCavesConfig _config;
    private readonly SceneStripper _stripper;
    private readonly HashSet<string> _processedScenes = new(StringComparer.OrdinalIgnoreCase);
    private Coroutine? _stripRoutine;

    public SceneStripCoordinator(
        MonoBehaviour host,
        ManualLogSource logger,
        DarkCavesConfig config,
        SceneStripper stripper)
    {
        _host = host;
        _logger = logger;
        _config = config;
        _stripper = stripper;
    }

    public void Stop()
    {
        if (_stripRoutine == null)
        {
            return;
        }

        _host.StopCoroutine(_stripRoutine);
        _stripRoutine = null;
    }

    public void QueueSceneStrip(
        Scene scene,
        string reason,
        bool forceRescan = false,
        bool singlePass = false)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (SceneUtils.IsIgnoredScene(scene.name))
        {
            _logger.LogInfo($"Skipping ignored scene '{scene.name}'.");
            return;
        }

        string sceneKey = SceneUtils.GetSceneKey(scene);
        if (!forceRescan && _processedScenes.Contains(sceneKey))
        {
            return;
        }

        _processedScenes.Add(sceneKey);

        if (_stripRoutine != null)
        {
            _host.StopCoroutine(_stripRoutine);
        }

        _stripRoutine = _host.StartCoroutine(StripSceneRoutine(scene, reason, singlePass));
    }

    private IEnumerator StripSceneRoutine(Scene scene, string reason, bool singlePass)
    {
        float totalDuration = Mathf.Max(0f, _config.ScanDurationSeconds);
        float interval = Mathf.Clamp(_config.ScanIntervalSeconds, 0.05f, 10f);
        int heavyInterval = Mathf.Max(1, _config.HeavyPassIntervalScans);
        float elapsed = 0f;
        StripRunStats stats = new();

        _logger.LogInfo($"Starting darkness pass for scene '{scene.name}' ({reason}).");

        while (scene.isLoaded && (singlePass ? stats.ScanCount < 1 : elapsed <= totalDuration + 0.0001f))
        {
            stats.ScanCount++;
            bool runHeavyPass = singlePass ||
                                stats.ScanCount == 1 ||
                                heavyInterval <= 1 ||
                                stats.ScanCount % heavyInterval == 0;

            stats.AddLights(_stripper.StripLightsInScene(scene));

            if (runHeavyPass)
            {
                stats.ReflectionProbesDisabled += _stripper.DisableReflectionProbesInScene(scene);
            }

            RendererStripStats rendererStats = runHeavyPass ? _stripper.StripRendererLightingInScene(scene) : default;
            stats.AddRenderer(rendererStats);

            if (runHeavyPass)
            {
                stats.AddDust(_stripper.StripDustVisualsInScene(scene));
                stats.ProjectorLikeComponentsDisabled += _stripper.DisableProjectorLikeComponentsInScene(scene);
                stats.PostProcessingDisabled += _stripper.DisablePostProcessingInScene(scene);
            }

            stats.AddTerrain(_stripper.StripTerrainInScene(scene));

            if (!stats.ClearedBakedAtLeastOnce || runHeavyPass)
            {
                bool clearedProbesThisScan;
                int clearedLightmapSlots = _stripper.TryClearBakedLighting(scene.name, out clearedProbesThisScan);
                stats.AddBakedLightingResult(clearedLightmapSlots, clearedProbesThisScan);
            }

            ApplyRenderSettings();

            if (singlePass)
            {
                break;
            }

            yield return new WaitForSecondsRealtime(interval);
            elapsed += interval;
        }

        _logger.LogInfo(stats.BuildSummary(scene.name, reason));
        _stripRoutine = null;
    }

    private static void ApplyRenderSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.ambientIntensity = 0f;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = false;
        RenderSettings.skybox = null;
    }

    private sealed class StripRunStats
    {
        public int ScanCount;
        public int TotalLightMatches;
        public int TotalLightDisabled;
        public int LightDriverBehavioursDisabled;
        public int ReflectionProbesDisabled;
        public int ProjectorLikeComponentsDisabled;
        public int RendererLightmapsCleared;
        public int RendererLightProbeUsageDisabled;
        public int RendererReflectionProbeUsageDisabled;
        public int DustParticleSystemsStopped;
        public int DustParticleRenderersDisabled;
        public int DustParticleLightsDisabled;
        public int DustVisualEffectsDisabled;
        public int TerrainMaterialsDarkened;
        public int TerrainLightmapsCleared;
        public int TerrainRealtimeLightmapsCleared;
        public int TerrainReflectionProbeUsageDisabled;
        public int TerrainSplatPropertyBlocksApplied;
        public int TerrainFoliageSuppressed;
        public int TerrainDataGrassSuppressed;
        public int PostProcessingDisabled;
        public int TotalLightmapSlotsCleared;
        public int LightProbeClearOps;
        public bool ClearedBakedAtLeastOnce;

        public void AddLights(LightStripStats lightStats)
        {
            TotalLightMatches += lightStats.Matched;
            TotalLightDisabled += lightStats.Disabled;
            LightDriverBehavioursDisabled += lightStats.DriverBehavioursDisabled;
        }

        public void AddRenderer(RendererStripStats rendererStats)
        {
            RendererLightmapsCleared += rendererStats.LightmapCleared;
            RendererLightProbeUsageDisabled += rendererStats.LightProbeUsageDisabled;
            RendererReflectionProbeUsageDisabled += rendererStats.ReflectionProbeUsageDisabled;
        }

        public void AddDust(DustStripStats dustStats)
        {
            DustParticleSystemsStopped += dustStats.ParticleSystemsStopped;
            DustParticleRenderersDisabled += dustStats.ParticleRenderersDisabled;
            DustParticleLightsDisabled += dustStats.ParticleLightsDisabled;
            DustVisualEffectsDisabled += dustStats.VisualEffectsDisabled;
        }

        public void AddTerrain(TerrainStripStats terrainStats)
        {
            TerrainMaterialsDarkened += terrainStats.TerrainMaterialsDarkened;
            TerrainLightmapsCleared += terrainStats.TerrainLightmapsCleared;
            TerrainRealtimeLightmapsCleared += terrainStats.TerrainRealtimeLightmapsCleared;
            TerrainReflectionProbeUsageDisabled += terrainStats.TerrainReflectionProbeUsageDisabled;
            TerrainSplatPropertyBlocksApplied += terrainStats.TerrainSplatPropertyBlocksApplied;
            TerrainFoliageSuppressed += terrainStats.TerrainFoliageSuppressed;
            TerrainDataGrassSuppressed += terrainStats.TerrainDataGrassSuppressed;
        }

        public void AddBakedLightingResult(int clearedLightmapSlots, bool clearedProbesThisScan)
        {
            TotalLightmapSlotsCleared += clearedLightmapSlots;
            if (clearedProbesThisScan)
            {
                LightProbeClearOps++;
            }

            if (clearedLightmapSlots > 0 || clearedProbesThisScan)
            {
                ClearedBakedAtLeastOnce = true;
            }
        }

        public string BuildSummary(string sceneName, string reason)
        {
            return
                $"Finished scene '{sceneName}' ({reason}) scans={ScanCount}, lightsMatched={TotalLightMatches}, lightsDisabled={TotalLightDisabled}, " +
                $"lightDriverScriptsOff={LightDriverBehavioursDisabled}, " +
                $"reflectionProbesDisabled={ReflectionProbesDisabled}, rendererLightmapsCleared={RendererLightmapsCleared}, rendererLightProbeOff={RendererLightProbeUsageDisabled}, " +
                $"rendererReflectionOff={RendererReflectionProbeUsageDisabled}, lightmapSlotsCleared={TotalLightmapSlotsCleared}, lightProbeClearOps={LightProbeClearOps}, " +
                $"dustStopped={DustParticleSystemsStopped}, dustRenderersOff={DustParticleRenderersDisabled}, dustParticleLightsOff={DustParticleLightsDisabled}, dustVfxOff={DustVisualEffectsDisabled}, " +
                $"projectorsOff={ProjectorLikeComponentsDisabled}, " +
                $"terrainMaterialsDarkened={TerrainMaterialsDarkened}, " +
                $"terrainLightmapsCleared={TerrainLightmapsCleared}, terrainRealtimeLightmapsCleared={TerrainRealtimeLightmapsCleared}, " +
                $"terrainReflectionProbeOff={TerrainReflectionProbeUsageDisabled}, terrainSplatBlocksApplied={TerrainSplatPropertyBlocksApplied}, " +
                $"terrainFoliageSuppressed={TerrainFoliageSuppressed}, terrainDataGrassSuppressed={TerrainDataGrassSuppressed}, " +
                $"postProcessingDisabled={PostProcessingDisabled}.";
        }
    }
}
