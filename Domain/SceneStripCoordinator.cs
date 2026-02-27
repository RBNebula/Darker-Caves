using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using DarkCaves.Configuration;
using DarkCaves.Services;
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
    private readonly SaveScopedRemovalTracker _saveScopedRemovalTracker;
    private readonly HashSet<string> _processedScenes = new(StringComparer.OrdinalIgnoreCase);
    private Coroutine? _stripRoutine;

    public SceneStripCoordinator(
        MonoBehaviour host,
        ManualLogSource logger,
        DarkCavesConfig config,
        SceneStripper stripper,
        SaveScopedRemovalTracker saveScopedRemovalTracker)
    {
        _host = host;
        _logger = logger;
        _config = config;
        _stripper = stripper;
        _saveScopedRemovalTracker = saveScopedRemovalTracker;
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
        if (!_config.Enabled)
        {
            return;
        }

        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (SceneUtils.IsIgnoredScene(scene.name, _config.IgnoreScenesCsv))
        {
            _logger.LogInfo($"Skipping ignored scene '{scene.name}'.");
            return;
        }

        string sceneKey = SceneUtils.GetSceneKey(scene);
        if (!forceRescan && _config.RunOncePerScene && _processedScenes.Contains(sceneKey))
        {
            return;
        }

        if (_config.RunOncePerScene)
        {
            _processedScenes.Add(sceneKey);
        }

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
        int scanCount = 0;
        int totalLightMatches = 0;
        int totalLightDisabled = 0;
        int totalLightDestroyed = 0;
        int lightDriverBehavioursDisabled = 0;
        int disabledReflectionProbes = 0;
        int projectorLikeComponentsDisabled = 0;
        int rendererLightmapsCleared = 0;
        int rendererLightProbeUsageDisabled = 0;
        int rendererReflectionProbeUsageDisabled = 0;
        int emissionRenderersModified = 0;
        int emissionMaterialsModified = 0;
        int dustParticleSystemsStopped = 0;
        int dustParticleRenderersDisabled = 0;
        int dustParticleLightsDisabled = 0;
        int dustVisualEffectsDisabled = 0;
        int terrainMaterialsDarkened = 0;
        int terrainRenderersDarkened = 0;
        int terrainSlotsDarkened = 0;
        int terrainLightmapsCleared = 0;
        int terrainRealtimeLightmapsCleared = 0;
        int terrainReflectionProbeUsageDisabled = 0;
        int terrainSplatPropertyBlocksApplied = 0;
        int terrainFoliageSuppressed = 0;
        int terrainDataGrassSuppressed = 0;
        int saveScopedObjectsRemoved = 0;
        int postProcessingDisabled = 0;
        int totalLightmapSlotsCleared = 0;
        int lightProbeClearOps = 0;
        bool clearedBakedAtLeastOnce = false;
        SaveScopedRemovalContext saveScopedContext = CreateSaveScopedRemovalContext();

        _logger.LogInfo($"Starting darkness pass for scene '{scene.name}' ({reason}).");

        while (scene.isLoaded && (singlePass ? scanCount < 1 : elapsed <= totalDuration + 0.0001f))
        {
            scanCount++;
            bool runHeavyPass = singlePass || scanCount == 1 || heavyInterval <= 1 || scanCount % heavyInterval == 0;

            saveScopedObjectsRemoved += ApplySaveScopedRemovalIfNeeded(scene, saveScopedContext);

            LightStripStats lightStats = _stripper.StripLightsInScene(scene);
            totalLightMatches += lightStats.Matched;
            totalLightDisabled += lightStats.Disabled;
            totalLightDestroyed += lightStats.Destroyed;
            lightDriverBehavioursDisabled += lightStats.DriverBehavioursDisabled;

            if (_config.DisableReflectionProbes && runHeavyPass)
            {
                disabledReflectionProbes += _stripper.DisableReflectionProbesInScene(scene);
            }

            RendererStripStats rendererStats = runHeavyPass ? _stripper.StripRendererLightingInScene(scene) : default;
            rendererLightmapsCleared += rendererStats.LightmapCleared;
            rendererLightProbeUsageDisabled += rendererStats.LightProbeUsageDisabled;
            rendererReflectionProbeUsageDisabled += rendererStats.ReflectionProbeUsageDisabled;

            if (_config.StripMaterialEmission && runHeavyPass)
            {
                EmissionStripStats emissionStats = _stripper.StripMaterialEmissionInScene(scene);
                emissionRenderersModified += emissionStats.RenderersModified;
                emissionMaterialsModified += emissionStats.MaterialsModified;
            }

            if ((_config.DisableDustParticleSystems || _config.DisableDustVisualEffects) && runHeavyPass)
            {
                DustStripStats dustStats = _stripper.StripDustVisualsInScene(scene);
                dustParticleSystemsStopped += dustStats.ParticleSystemsStopped;
                dustParticleRenderersDisabled += dustStats.ParticleRenderersDisabled;
                dustParticleLightsDisabled += dustStats.ParticleLightsDisabled;
                dustVisualEffectsDisabled += dustStats.VisualEffectsDisabled;
            }

            if (_config.DisableProjectorLikeComponents && runHeavyPass)
            {
                projectorLikeComponentsDisabled += _stripper.DisableProjectorLikeComponentsInScene(scene);
            }

            if (_config.DarkenTerrainMaterials || _config.StripRendererLightmaps || _config.DisableRendererProbeUsage)
            {
                TerrainStripStats terrainStats = _stripper.StripTerrainInScene(
                    scene,
                    includeRendererPass: runHeavyPass && _config.DarkenTerrainMaterials);
                terrainMaterialsDarkened += terrainStats.TerrainMaterialsDarkened;
                terrainRenderersDarkened += terrainStats.TerrainRenderersDarkened;
                terrainSlotsDarkened += terrainStats.TerrainSlotsDarkened;
                terrainLightmapsCleared += terrainStats.TerrainLightmapsCleared;
                terrainRealtimeLightmapsCleared += terrainStats.TerrainRealtimeLightmapsCleared;
                terrainReflectionProbeUsageDisabled += terrainStats.TerrainReflectionProbeUsageDisabled;
                terrainSplatPropertyBlocksApplied += terrainStats.TerrainSplatPropertyBlocksApplied;
                terrainFoliageSuppressed += terrainStats.TerrainFoliageSuppressed;
                terrainDataGrassSuppressed += terrainStats.TerrainDataGrassSuppressed;
            }

            if (_config.DisablePostProcessing && runHeavyPass)
            {
                postProcessingDisabled += _stripper.DisablePostProcessingInScene(scene);
            }

            if (_config.ClearBakedLightmaps && (!clearedBakedAtLeastOnce || (_config.ReapplyBakedClearEachScan && runHeavyPass)))
            {
                bool clearedProbesThisScan;
                int clearedLightmapSlots = _stripper.TryClearBakedLighting(scene.name, out clearedProbesThisScan);
                totalLightmapSlotsCleared += clearedLightmapSlots;
                if (clearedProbesThisScan)
                {
                    lightProbeClearOps++;
                }

                if (clearedLightmapSlots > 0 || clearedProbesThisScan)
                {
                    clearedBakedAtLeastOnce = true;
                }
            }

            ApplyRenderSettings();

            if (singlePass)
            {
                break;
            }

            yield return new WaitForSecondsRealtime(interval);
            elapsed += interval;
        }

        _logger.LogInfo(
            $"Finished scene '{scene.name}' ({reason}) scans={scanCount}, lightsMatched={totalLightMatches}, lightsDisabled={totalLightDisabled}, lightsDestroyed={totalLightDestroyed}, " +
            $"lightDriverScriptsOff={lightDriverBehavioursDisabled}, " +
            $"reflectionProbesDisabled={disabledReflectionProbes}, rendererLightmapsCleared={rendererLightmapsCleared}, rendererLightProbeOff={rendererLightProbeUsageDisabled}, " +
            $"rendererReflectionOff={rendererReflectionProbeUsageDisabled}, lightmapSlotsCleared={totalLightmapSlotsCleared}, lightProbeClearOps={lightProbeClearOps}, " +
            $"emissionRenderers={emissionRenderersModified}, emissionMaterials={emissionMaterialsModified}, " +
            $"dustStopped={dustParticleSystemsStopped}, dustRenderersOff={dustParticleRenderersDisabled}, dustParticleLightsOff={dustParticleLightsDisabled}, dustVfxOff={dustVisualEffectsDisabled}, " +
            $"projectorsOff={projectorLikeComponentsDisabled}, " +
            $"terrainMaterialsDarkened={terrainMaterialsDarkened}, terrainRenderersDarkened={terrainRenderersDarkened}, terrainSlotsDarkened={terrainSlotsDarkened}, " +
            $"terrainLightmapsCleared={terrainLightmapsCleared}, terrainRealtimeLightmapsCleared={terrainRealtimeLightmapsCleared}, " +
            $"terrainReflectionProbeOff={terrainReflectionProbeUsageDisabled}, terrainSplatBlocksApplied={terrainSplatPropertyBlocksApplied}, " +
            $"terrainFoliageSuppressed={terrainFoliageSuppressed}, terrainDataGrassSuppressed={terrainDataGrassSuppressed}, " +
            $"saveScopedObjectsRemoved={saveScopedObjectsRemoved}, " +
            $"postProcessingDisabled={postProcessingDisabled}.");

        MarkSaveScopedRemovalIfNeeded(saveScopedContext, saveScopedObjectsRemoved);

        _stripRoutine = null;
    }

    private SaveScopedRemovalContext CreateSaveScopedRemovalContext()
    {
        if (!_config.EnableSaveScopedOneTimeRemoval)
        {
            return SaveScopedRemovalContext.Disabled;
        }

        HashSet<int> targetIds = SavableObjectIdCsvParser.Parse(_config.SaveScopedRemoveIdsCsv);
        if (targetIds.Count == 0)
        {
            return SaveScopedRemovalContext.Disabled;
        }

        string activeSaveKey = _saveScopedRemovalTracker.GetActiveSaveKey();
        if (activeSaveKey.Length == 0 || _saveScopedRemovalTracker.HasProcessed(activeSaveKey))
        {
            return SaveScopedRemovalContext.Disabled;
        }

        return new SaveScopedRemovalContext(activeSaveKey, targetIds);
    }

    private int ApplySaveScopedRemovalIfNeeded(Scene scene, SaveScopedRemovalContext context)
    {
        if (!context.IsEnabled)
        {
            return 0;
        }

        return _stripper.RemoveSaveScopedObjectsInScene(scene, context.TargetIds);
    }

    private void MarkSaveScopedRemovalIfNeeded(SaveScopedRemovalContext context, int removedCount)
    {
        if (!context.IsEnabled)
        {
            return;
        }

        bool marked = _saveScopedRemovalTracker.MarkProcessed(context.SaveKey);
        _logger.LogInfo(
            $"Save-scoped one-time removal {(marked ? "completed" : "already-complete")} for save '{context.SaveKey}': " +
            $"removed={removedCount}, ids='{_config.SaveScopedRemoveIdsCsv}'.");
    }

    private void ApplyRenderSettings()
    {
        if (_config.ForceAmbientBlack)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
            RenderSettings.ambientIntensity = 0f;
        }

        if (_config.ZeroReflectionIntensity)
        {
            RenderSettings.reflectionIntensity = 0f;
        }

        if (_config.DisableFog)
        {
            RenderSettings.fog = false;
        }

        if (_config.DisableSkybox)
        {
            RenderSettings.skybox = null;
        }
    }

    private readonly struct SaveScopedRemovalContext
    {
        public static SaveScopedRemovalContext Disabled => new(string.Empty, _emptySet);

        private static readonly HashSet<int> _emptySet = new();

        public SaveScopedRemovalContext(string saveKey, HashSet<int> targetIds)
        {
            SaveKey = saveKey;
            TargetIds = targetIds;
        }

        public string SaveKey { get; }
        public HashSet<int> TargetIds { get; }
        public bool IsEnabled => SaveKey.Length > 0 && TargetIds.Count > 0;
    }
}
