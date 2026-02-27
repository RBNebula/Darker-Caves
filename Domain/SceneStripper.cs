using System;
using System.Collections.Generic;
using BepInEx.Logging;
using DarkCaves.Configuration;
using DarkCaves.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DarkCaves.Domain;

internal sealed class SceneStripper
{
    private readonly DarkCavesConfig _config;
    private readonly ManualLogSource _logger;

    public SceneStripper(DarkCavesConfig config, ManualLogSource logger)
    {
        _config = config;
        _logger = logger;
    }

    public LightStripStats StripLightsInScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        LightStripStats stats = default;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            Light[] lights = root.GetComponentsInChildren<Light>(true);
            for (int j = 0; j < lights.Length; j++)
            {
                Light light = lights[j];
                TryTunePreservedPlayerLight(light);
                if (light == null || !ShouldAffectLight(light))
                {
                    continue;
                }

                stats.Matched++;
                if (_config.DisableLightDriverBehaviours)
                {
                    stats.DriverBehavioursDisabled += DisableLightDriverBehaviours(light.gameObject);
                }

                bool destroyed = false;
                bool shouldTryDestroy = _config.DestroyLightComponents;
                if (shouldTryDestroy && !LightDependencyInspector.HasLightDependencyOnSameGameObject(light.gameObject))
                {
                    try
                    {
                        UnityEngine.Object.Destroy(light);
                        destroyed = true;
                        stats.Destroyed++;
                    }
                    catch
                    {
                        destroyed = false;
                    }
                }

                if (!destroyed)
                {
                    if (!light.enabled &&
                        Mathf.Approximately(light.intensity, 0f) &&
                        Mathf.Approximately(light.bounceIntensity, 0f) &&
                        light.shadows == LightShadows.None)
                    {
                        continue;
                    }

                    light.enabled = false;
                    light.intensity = 0f;
                    light.bounceIntensity = 0f;
                    light.shadows = LightShadows.None;
                    stats.Disabled++;
                }
            }
        }

        return stats;
    }

    public int RemoveSaveScopedObjectsInScene(Scene scene, ISet<int> targetSavableObjectIds)
    {
        if (targetSavableObjectIds == null || targetSavableObjectIds.Count == 0)
        {
            return 0;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        HashSet<int> removedInstanceIds = new();
        int removedCount = 0;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int j = 0; j < behaviours.Length; j++)
            {
                MonoBehaviour behaviour = behaviours[j];
                if (behaviour == null || behaviour is not ISaveLoadableObject saveLoadableObject)
                {
                    continue;
                }

                int idValue;
                try
                {
                    idValue = (int)saveLoadableObject.GetSavableObjectID();
                }
                catch
                {
                    continue;
                }

                if (!targetSavableObjectIds.Contains(idValue))
                {
                    continue;
                }

                Component? saveComponent = saveLoadableObject as Component;
                GameObject? objectToRemove = saveComponent != null ? saveComponent.gameObject : behaviour.gameObject;
                if (objectToRemove == null)
                {
                    continue;
                }

                int instanceId = objectToRemove.GetInstanceID();
                if (!removedInstanceIds.Add(instanceId))
                {
                    continue;
                }

                UnityEngine.Object.Destroy(objectToRemove);
                removedCount++;
            }
        }

        return removedCount;
    }

    public int DisableReflectionProbesInScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        int count = 0;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            ReflectionProbe[] probes = root.GetComponentsInChildren<ReflectionProbe>(true);
            for (int j = 0; j < probes.Length; j++)
            {
                ReflectionProbe probe = probes[j];
                if (probe == null)
                {
                    continue;
                }

                if (!probe.enabled && Mathf.Approximately(probe.intensity, 0f))
                {
                    continue;
                }

                probe.enabled = false;
                probe.intensity = 0f;
                count++;
            }
        }

        return count;
    }

    public int DisablePostProcessingInScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        int count = 0;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
            for (int j = 0; j < behaviours.Length; j++)
            {
                Behaviour behaviour = behaviours[j];
                if (behaviour == null || !behaviour.enabled)
                {
                    continue;
                }

                string fullName = behaviour.GetType().FullName ?? behaviour.GetType().Name;
                if (!ComponentTypeFilters.IsPostProcessingComponent(fullName))
                {
                    continue;
                }

                behaviour.enabled = false;
                count++;
            }
        }

        return count;
    }

    public int DisableProjectorLikeComponentsInScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        int count = 0;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
            for (int j = 0; j < behaviours.Length; j++)
            {
                Behaviour behaviour = behaviours[j];
                if (behaviour == null || !behaviour.enabled || ShouldSkipRendererEmission(behaviour.transform))
                {
                    continue;
                }

                string fullName = behaviour.GetType().FullName ?? behaviour.GetType().Name;
                if (!ComponentTypeFilters.IsProjectorLikeComponent(fullName))
                {
                    continue;
                }

                behaviour.enabled = false;
                count++;
            }
        }

        return count;
    }

    public RendererStripStats StripRendererLightingInScene(Scene scene)
    {
        RendererStripStats stats = default;
        if (!_config.StripRendererLightmaps && !_config.DisableRendererProbeUsage)
        {
            return stats;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null)
                {
                    continue;
                }

                if (_config.StripRendererLightmaps && renderer.lightmapIndex >= 0)
                {
                    renderer.lightmapIndex = -1;
                    stats.LightmapCleared++;
                }

                if (_config.StripRendererLightmaps && renderer.realtimeLightmapIndex >= 0)
                {
                    renderer.realtimeLightmapIndex = -1;
                    stats.LightmapCleared++;
                }

                if (_config.DisableRendererProbeUsage && renderer.lightProbeUsage != LightProbeUsage.Off)
                {
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    stats.LightProbeUsageDisabled++;
                }

                if (_config.DisableRendererProbeUsage && renderer.reflectionProbeUsage != ReflectionProbeUsage.Off)
                {
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    stats.ReflectionProbeUsageDisabled++;
                }
            }
        }

        return stats;
    }

    public EmissionStripStats StripMaterialEmissionInScene(Scene scene)
    {
        EmissionStripStats stats = default;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null || ShouldSkipRendererEmission(renderer.transform))
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                bool rendererChanged = false;
                MaterialPropertyBlock block = new();
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null)
                    {
                        continue;
                    }

                    try
                    {
                        bool slotChanged = false;
                        renderer.GetPropertyBlock(block, m);

                        if (material.HasProperty("_EmissionColor"))
                        {
                            Color emissionColor = material.GetColor("_EmissionColor");
                            if (!MaterialPropertyUtils.IsNearBlack(emissionColor))
                            {
                                slotChanged = true;
                            }

                            block.SetColor("_EmissionColor", Color.black);
                        }

                        if (material.HasProperty("_EmissionMap"))
                        {
                            Texture currentMap = material.GetTexture("_EmissionMap");
                            if (currentMap != null)
                            {
                                slotChanged = true;
                            }

                            block.SetTexture("_EmissionMap", Texture2D.blackTexture);
                        }

                        slotChanged |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissionStrength");
                        slotChanged |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissiveIntensity");
                        slotChanged |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissiveExposureWeight");
                        slotChanged |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_Glow");
                        slotChanged |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_GlowPower");

                        if (material.IsKeywordEnabled("_EMISSION"))
                        {
                            material.DisableKeyword("_EMISSION");
                            slotChanged = true;
                        }

                        if ((material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0)
                        {
                            material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                            slotChanged = true;
                        }

                        if (slotChanged)
                        {
                            renderer.SetPropertyBlock(block, m);
                            stats.MaterialsModified++;
                            rendererChanged = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Emission strip failed on renderer '{renderer.name}' materialIndex={m} scene='{scene.name}': {ex.Message}");
                    }
                    finally
                    {
                        block.Clear();
                    }
                }

                if (rendererChanged)
                {
                    stats.RenderersModified++;
                }
            }
        }

        return stats;
    }

    public DustStripStats StripDustVisualsInScene(Scene scene)
    {
        DustStripStats stats = default;
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            if (_config.DisableDustParticleSystems)
            {
                ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
                for (int j = 0; j < particleSystems.Length; j++)
                {
                    ParticleSystem particleSystem = particleSystems[j];
                    if (particleSystem == null || ShouldSkipRendererEmission(particleSystem.transform))
                    {
                        continue;
                    }

                    bool dustByName = KeywordMatcher.HasKeywordInHierarchy(particleSystem.transform, _config.DustKeywordsCsv) ||
                                      KeywordMatcher.ContainsCsvToken(particleSystem.name, _config.DustKeywordsCsv);
                    ParticleSystem.LightsModule lights = particleSystem.lights;
                    if (!dustByName)
                    {
                        continue;
                    }

                    bool changed = false;
                    ParticleSystem.EmissionModule emission = particleSystem.emission;
                    if (emission.enabled)
                    {
                        emission.enabled = false;
                        changed = true;
                    }

                    if (lights.enabled)
                    {
                        lights.enabled = false;
                        stats.ParticleLightsDisabled++;
                        changed = true;
                    }

                    ParticleSystemRenderer? renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null && renderer.enabled)
                    {
                        renderer.enabled = false;
                        stats.ParticleRenderersDisabled++;
                        changed = true;
                    }

                    if (changed || particleSystem.isPlaying || particleSystem.particleCount > 0)
                    {
                        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        stats.ParticleSystemsStopped++;
                    }
                }
            }

            if (_config.DisableDustVisualEffects)
            {
                Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
                for (int j = 0; j < behaviours.Length; j++)
                {
                    Behaviour behaviour = behaviours[j];
                    if (behaviour == null || !behaviour.enabled || ShouldSkipRendererEmission(behaviour.transform))
                    {
                        continue;
                    }

                    string fullName = behaviour.GetType().FullName ?? behaviour.GetType().Name;
                    bool dustByName = KeywordMatcher.HasKeywordInHierarchy(behaviour.transform, _config.DustKeywordsCsv) ||
                                      KeywordMatcher.ContainsCsvToken(fullName, _config.DustKeywordsCsv) ||
                                      KeywordMatcher.ContainsCsvToken(behaviour.name, _config.DustKeywordsCsv);
                    if (!dustByName)
                    {
                        continue;
                    }

                    if (fullName.IndexOf("VisualEffect", StringComparison.OrdinalIgnoreCase) < 0 &&
                        fullName.IndexOf("VFX", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    behaviour.enabled = false;
                    stats.VisualEffectsDisabled++;
                }
            }
        }

        return stats;
    }

    public TerrainStripStats StripTerrainInScene(Scene scene, bool includeRendererPass)
    {
        TerrainStripStats stats = default;
        GameObject[] roots = scene.GetRootGameObjects();
        float multiplier = Mathf.Clamp01(_config.TerrainColorMultiplier);
        bool affectAll = _config.TerrainAffectAllRenderers;

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root == null)
            {
                continue;
            }

            Terrain[] terrains = root.GetComponentsInChildren<Terrain>(true);
            for (int j = 0; j < terrains.Length; j++)
            {
                Terrain terrain = terrains[j];
                if (terrain == null || ShouldSkipRendererEmission(terrain.transform))
                {
                    continue;
                }

                if (_config.StripRendererLightmaps && terrain.lightmapIndex >= 0)
                {
                    terrain.lightmapIndex = -1;
                    stats.TerrainLightmapsCleared++;
                }

                if (_config.StripRendererLightmaps && terrain.realtimeLightmapIndex >= 0)
                {
                    terrain.realtimeLightmapIndex = -1;
                    stats.TerrainRealtimeLightmapsCleared++;
                }

                if (_config.DisableRendererProbeUsage && terrain.reflectionProbeUsage != ReflectionProbeUsage.Off)
                {
                    terrain.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    stats.TerrainReflectionProbeUsageDisabled++;
                }

                if (_config.SuppressTerrainFoliage)
                {
                    bool terrainDataChanged;
                    if (SuppressTerrainFoliage(terrain, out terrainDataChanged))
                    {
                        stats.TerrainFoliageSuppressed++;
                    }

                    if (terrainDataChanged)
                    {
                        stats.TerrainDataGrassSuppressed++;
                    }
                }

                Material? terrainMaterial = terrain.materialTemplate;
                if (_config.DarkenTerrainMaterials &&
                    terrainMaterial != null &&
                    DarkenTerrainMaterial(terrainMaterial, multiplier))
                {
                    stats.TerrainMaterialsDarkened++;
                }

                if (_config.DarkenTerrainMaterials &&
                    TryDarkenTerrainSplatPropertyBlock(terrain, terrainMaterial, multiplier, scene.name))
                {
                    stats.TerrainSplatPropertyBlocksApplied++;
                }
            }

            if (!includeRendererPass || !_config.DarkenTerrainMaterials)
            {
                continue;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null || ShouldSkipRendererEmission(renderer.transform))
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                bool rendererChanged = false;
                MaterialPropertyBlock block = new();
                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null)
                    {
                        continue;
                    }

                    if (!affectAll && !IsTerrainLikeRenderer(renderer, material))
                    {
                        continue;
                    }

                    try
                    {
                        bool slotChanged = DarkenTerrainMaterial(material, multiplier);
                        bool slotOverrideChanged = DarkenTerrainRendererSlotOverride(renderer, m, material, block, multiplier);
                        bool resetBlocks = _config.TerrainResetPropertyBlocks;
                        if (slotOverrideChanged || resetBlocks)
                        {
                            renderer.SetPropertyBlock(block, m);
                            slotChanged = true;
                        }

                        if (slotChanged)
                        {
                            stats.TerrainSlotsDarkened++;
                            rendererChanged = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Terrain darken failed on renderer '{renderer.name}' materialIndex={m} scene='{scene.name}': {ex.Message}");
                    }
                    finally
                    {
                        block.Clear();
                    }
                }

                if (rendererChanged)
                {
                    stats.TerrainRenderersDarkened++;
                }
            }
        }

        return stats;
    }

    public int TryClearBakedLighting(string sceneName, out bool lightProbesCleared)
    {
        lightProbesCleared = false;
        try
        {
            int lightmapCount = LightmapSettings.lightmaps?.Length ?? 0;
            LightmapSettings.lightmaps = Array.Empty<LightmapData>();

            if (_config.ClearLightProbes && LightmapSettings.lightProbes != null)
            {
                LightmapSettings.lightProbes = null;
                lightProbesCleared = true;
            }

            DynamicGI.UpdateEnvironment();
            return lightmapCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to clear baked lighting in '{sceneName}': {ex.Message}");
            return 0;
        }
    }

    private bool ShouldAffectLight(Light light)
    {
        Transform transform = light.transform;

        if (_config.PreserveLanternLights && KeywordMatcher.HasKeywordInHierarchy(transform, _config.LanternKeywordsCsv))
        {
            return false;
        }

        if (_config.PreservePlayerLights && KeywordMatcher.HasKeywordInHierarchy(transform, _config.PlayerKeywordsCsv))
        {
            return false;
        }

        if (_config.RemoveDustMoteLights && KeywordMatcher.HasKeywordInHierarchy(transform, _config.DustKeywordsCsv))
        {
            return true;
        }

        if (_config.TargetPointLightsOnly && light.type != LightType.Point)
        {
            return false;
        }

        if (_config.RemoveOnlyOrphanPointLights && light.type == LightType.Point)
        {
            return transform.parent == null;
        }

        return true;
    }

    private void TryTunePreservedPlayerLight(Light? light)
    {
        if (light == null || !_config.PreservePlayerLights)
        {
            return;
        }

        if (!KeywordMatcher.HasKeywordInHierarchy(light.transform, _config.PlayerKeywordsCsv))
        {
            return;
        }

        string lightName = light.name ?? string.Empty;
        if (lightName.IndexOf("playerlight", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return;
        }

        const float targetIntensity = 0.2f;
        if (Mathf.Abs(light.intensity - targetIntensity) <= 0.0001f)
        {
            return;
        }

        light.intensity = targetIntensity;
    }

    private bool ShouldSkipRendererEmission(Transform transform)
    {
        if (_config.PreserveLanternLights && KeywordMatcher.HasKeywordInHierarchy(transform, _config.LanternKeywordsCsv))
        {
            return true;
        }

        if (_config.PreservePlayerLights && KeywordMatcher.HasKeywordInHierarchy(transform, _config.PlayerKeywordsCsv))
        {
            return true;
        }

        return false;
    }

    private int DisableLightDriverBehaviours(GameObject gameObject)
    {
        Behaviour[] behaviours = gameObject.GetComponents<Behaviour>();
        int count = 0;
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled)
            {
                continue;
            }

            Type type = behaviour.GetType();
            if (type == typeof(Light))
            {
                continue;
            }

            if (!ComponentTypeFilters.IsLikelyLightDriver(type) &&
                !LightDependencyInspector.RequiresLightByAttribute(type))
            {
                continue;
            }

            behaviour.enabled = false;
            count++;
        }

        return count;
    }

    private bool DarkenTerrainMaterial(Material material, float multiplier)
    {
        bool forceBlack = _config.TerrainForceBlack;
        float colorMultiplier = forceBlack ? 0f : multiplier;
        bool changed = false;
        changed |= MaterialPropertyUtils.TryScaleColorPropertyDirect(material, "_BaseColor", colorMultiplier);
        changed |= MaterialPropertyUtils.TryScaleColorPropertyDirect(material, "_Color", colorMultiplier);
        changed |= MaterialPropertyUtils.TryScaleColorPropertyDirect(material, "_Tint", colorMultiplier);
        changed |= MaterialPropertyUtils.TryScaleColorPropertyDirect(material, "_TerrainColor", colorMultiplier);
        changed |= MaterialPropertyUtils.TryScaleColorPropertyDirect(material, "_LayerColor", colorMultiplier);
        changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_EmissionColor", Color.black);
        changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_EmissionMap", Texture2D.blackTexture);
        changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_EmissionStrength");
        changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_EmissiveIntensity");

        if (forceBlack)
        {
            changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_BaseColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_Color", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_Tint", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_TerrainColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_LayerColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_SpecColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_ReflectColor", Color.black);

            changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_Glossiness");
            changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_Smoothness");
            changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_Metallic");
            changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_SpecularHighlights");
            changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_EnvironmentReflections");
            changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_OcclusionStrength");

            if (_config.TerrainForceBlackTextures)
            {
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_BaseMap", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_MainTex", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_BaseColorMap", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_Albedo", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_Diffuse", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_TerrainTex", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_Splat0", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_Splat1", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_Splat2", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_Splat3", Texture2D.blackTexture);
            }

            changed |= MaterialPropertyUtils.TryForceDarkenByShaderHeuristics(material, _config.TerrainForceBlackTextures);
        }

        if (material.IsKeywordEnabled("_EMISSION"))
        {
            material.DisableKeyword("_EMISSION");
            changed = true;
        }

        if ((material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0)
        {
            material.globalIlluminationFlags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            changed = true;
        }

        return changed;
    }

    private bool SuppressTerrainFoliage(Terrain terrain, out bool terrainDataChanged)
    {
        terrainDataChanged = false;
        bool changed = false;

        try
        {
            if (terrain.drawTreesAndFoliage)
            {
                terrain.drawTreesAndFoliage = false;
                changed = true;
            }

            if (terrain.detailObjectDistance > 0f)
            {
                terrain.detailObjectDistance = 0f;
                changed = true;
            }

            if (terrain.detailObjectDensity > 0f)
            {
                terrain.detailObjectDensity = 0f;
                changed = true;
            }

            if (terrain.treeDistance > 0f)
            {
                terrain.treeDistance = 0f;
                changed = true;
            }

            if (terrain.treeBillboardDistance > 0f)
            {
                terrain.treeBillboardDistance = 0f;
                changed = true;
            }

            if (terrain.treeCrossFadeLength > 0f)
            {
                terrain.treeCrossFadeLength = 0f;
                changed = true;
            }

            if (terrain.treeMaximumFullLODCount != 0)
            {
                terrain.treeMaximumFullLODCount = 0;
                changed = true;
            }

            TerrainData? data = terrain.terrainData;
            if (data != null)
            {
                bool dataChanged = false;
                if (data.wavingGrassStrength > 0f)
                {
                    data.wavingGrassStrength = 0f;
                    dataChanged = true;
                }

                if (data.wavingGrassAmount > 0f)
                {
                    data.wavingGrassAmount = 0f;
                    dataChanged = true;
                }

                if (data.wavingGrassSpeed > 0f)
                {
                    data.wavingGrassSpeed = 0f;
                    dataChanged = true;
                }

                Color tint = data.wavingGrassTint;
                if (!MaterialPropertyUtils.IsNearBlack(tint))
                {
                    data.wavingGrassTint = Color.black;
                    dataChanged = true;
                }

                if (dataChanged)
                {
                    terrainDataChanged = true;
                }
            }

            if (changed)
            {
                terrain.Flush();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Terrain foliage suppression failed on terrain '{terrain.name}': {ex.Message}");
            return false;
        }

        return changed;
    }

    private bool TryDarkenTerrainSplatPropertyBlock(Terrain terrain, Material? terrainMaterial, float multiplier, string sceneName)
    {
        try
        {
            MaterialPropertyBlock block = new();
            bool resetBlocks = _config.TerrainResetPropertyBlocks;
            if (!resetBlocks)
            {
                terrain.GetSplatMaterialPropertyBlock(block);
            }

            bool changed = DarkenTerrainSplatPropertyBlock(terrainMaterial, block, multiplier);
            if (!changed && !resetBlocks)
            {
                return false;
            }

            terrain.SetSplatMaterialPropertyBlock(block);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Terrain splat darken failed on terrain '{terrain.name}' scene='{sceneName}': {ex.Message}");
            return false;
        }
    }

    private bool DarkenTerrainSplatPropertyBlock(Material? material, MaterialPropertyBlock block, float multiplier)
    {
        bool forceBlack = _config.TerrainForceBlack;
        float colorMultiplier = forceBlack ? 0f : multiplier;
        bool changed = false;

        if (material != null)
        {
            changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_BaseColor", colorMultiplier);
            changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_Color", colorMultiplier);
            changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_Tint", colorMultiplier);
            changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_TerrainColor", colorMultiplier);
            changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_LayerColor", colorMultiplier);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_EmissionColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_EmissionMap", Texture2D.blackTexture);
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissionStrength");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissiveIntensity");
        }

        if (!forceBlack)
        {
            return changed;
        }

        if (material != null)
        {
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_BaseColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_Color", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_Tint", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_TerrainColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_LayerColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_SpecColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_ReflectColor", Color.black);
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_Glossiness");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_Smoothness");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_Metallic");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_SpecularHighlights");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EnvironmentReflections");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_OcclusionStrength");

            if (_config.TerrainForceBlackTextures)
            {
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_BaseMap", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_MainTex", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_BaseColorMap", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Albedo", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Diffuse", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_TerrainTex", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Splat0", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Splat1", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Splat2", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Splat3", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Control", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Control0", Texture2D.blackTexture);
            }

            changed |= MaterialPropertyUtils.TryForceDarkenByShaderHeuristics(material, block, _config.TerrainForceBlackTextures);
        }

        return changed;
    }

    private bool DarkenTerrainRendererSlotOverride(
        Renderer renderer,
        int materialIndex,
        Material material,
        MaterialPropertyBlock block,
        float multiplier)
    {
        bool resetBlocks = _config.TerrainResetPropertyBlocks;
        if (!resetBlocks)
        {
            renderer.GetPropertyBlock(block, materialIndex);
        }

        bool forceBlack = _config.TerrainForceBlack;
        float colorMultiplier = forceBlack ? 0f : multiplier;
        bool changed = false;
        changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_BaseColor", colorMultiplier);
        changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_Color", colorMultiplier);
        changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_Tint", colorMultiplier);
        changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_TerrainColor", colorMultiplier);
        changed |= MaterialPropertyUtils.TryScaleColorProperty(material, block, "_LayerColor", colorMultiplier);
        changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_EmissionColor", Color.black);
        changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_EmissionMap", Texture2D.blackTexture);
        changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissionStrength");
        changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissiveIntensity");

        if (forceBlack)
        {
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_BaseColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_Color", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_Tint", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_TerrainColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_LayerColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_SpecColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_ReflectColor", Color.black);

            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_Glossiness");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_Smoothness");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_Metallic");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_SpecularHighlights");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EnvironmentReflections");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_OcclusionStrength");

            if (_config.TerrainForceBlackTextures)
            {
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_BaseMap", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_MainTex", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_BaseColorMap", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Albedo", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Diffuse", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_TerrainTex", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Splat0", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Splat1", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Splat2", Texture2D.blackTexture);
                changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_Splat3", Texture2D.blackTexture);
            }

            changed |= MaterialPropertyUtils.TryForceDarkenByShaderHeuristics(material, block, _config.TerrainForceBlackTextures);
        }

        return changed;
    }

    private bool IsTerrainLikeRenderer(Renderer renderer, Material material)
    {
        if (KeywordMatcher.HasKeywordInHierarchy(renderer.transform, _config.TerrainKeywordsCsv))
        {
            return true;
        }

        if (KeywordMatcher.ContainsCsvToken(renderer.name, _config.TerrainKeywordsCsv))
        {
            return true;
        }

        if (KeywordMatcher.ContainsCsvToken(material.name, _config.TerrainKeywordsCsv))
        {
            return true;
        }

        Shader? shader = material.shader;
        if (shader != null && KeywordMatcher.ContainsCsvToken(shader.name, _config.TerrainKeywordsCsv))
        {
            return true;
        }

        if (!_config.TerrainUseHeuristicMatching)
        {
            return false;
        }

        if (IsHeuristicTerrainMaterial(material))
        {
            return true;
        }

        return IsHeuristicGroundSurface(renderer);
    }

    private static bool IsHeuristicTerrainMaterial(Material material)
    {
        Shader? shader = material.shader;
        string shaderName = shader?.name ?? string.Empty;
        if (shaderName.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0 ||
            shaderName.IndexOf("Splat", StringComparison.OrdinalIgnoreCase) >= 0 ||
            shaderName.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0 ||
            shaderName.IndexOf("Nature", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (material.HasProperty("_Control") ||
            material.HasProperty("_Splat0") ||
            material.HasProperty("_Splat1") ||
            material.HasProperty("_Splat2") ||
            material.HasProperty("_Splat3") ||
            material.HasProperty("_TerrainHolesTexture") ||
            material.HasProperty("_TerrainNormalMap"))
        {
            return true;
        }

        return false;
    }

    private bool IsHeuristicGroundSurface(Renderer renderer)
    {
        Bounds bounds = renderer.bounds;
        Vector3 size = bounds.size;
        float areaXY = Mathf.Abs(size.x * size.y);
        float areaXZ = Mathf.Abs(size.x * size.z);
        float areaYZ = Mathf.Abs(size.y * size.z);
        float maxArea = Mathf.Max(areaXY, Mathf.Max(areaXZ, areaYZ));
        float minAxis = Mathf.Min(size.x, Mathf.Min(size.y, size.z));

        if (maxArea < Mathf.Max(0f, _config.TerrainHeuristicMinSurfaceArea))
        {
            return false;
        }

        if (minAxis > Mathf.Max(0.1f, _config.TerrainHeuristicMaxThickness))
        {
            return false;
        }

        return true;
    }
}
