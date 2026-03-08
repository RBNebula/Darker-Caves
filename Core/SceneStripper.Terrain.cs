using System;
using DarkCaves.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DarkCaves.Core;

internal sealed partial class SceneStripper
{
    public TerrainStripStats StripTerrainInScene(Scene scene)
    {
        TerrainStripStats stats = default;
        GameObject[] roots = scene.GetRootGameObjects();

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
                if (terrain == null || IsExcludedFromTargeting(terrain.transform))
                {
                    continue;
                }

                if (terrain.lightmapIndex >= 0)
                {
                    terrain.lightmapIndex = -1;
                    stats.TerrainLightmapsCleared++;
                }

                if (terrain.realtimeLightmapIndex >= 0)
                {
                    terrain.realtimeLightmapIndex = -1;
                    stats.TerrainRealtimeLightmapsCleared++;
                }

                bool terrainDataChanged;
                if (SuppressTerrainFoliage(terrain, out terrainDataChanged))
                {
                    stats.TerrainFoliageSuppressed++;
                }

                if (terrainDataChanged)
                {
                    stats.TerrainDataGrassSuppressed++;
                }

                Material? terrainMaterial = terrain.materialTemplate;
                if (terrainMaterial != null && DarkenTerrainMaterial(terrainMaterial))
                {
                    stats.TerrainMaterialsDarkened++;
                }

                if (TryDarkenTerrainSplatPropertyBlock(terrain, terrainMaterial, scene.name))
                {
                    stats.TerrainSplatPropertyBlocksApplied++;
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

            if (LightmapSettings.lightProbes != null)
            {
                LightmapSettings.lightProbes = null;
                lightProbesCleared = true;
            }

            DynamicGI.UpdateEnvironment();
            return lightmapCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"{ModInfo.LOG_PREFIX} Failed to clear baked lighting in '{sceneName}': {ex.Message}");
            return 0;
        }
    }

    private static bool DarkenTerrainMaterial(Material material)
    {
        bool changed = false;
        changed |= MaterialPropertyUtils.TrySetColorPropertyDirect(material, "_EmissionColor", Color.black);
        changed |= MaterialPropertyUtils.TrySetTexturePropertyDirect(material, "_EmissionMap", Texture2D.blackTexture);
        changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_EmissionStrength");
        changed |= MaterialPropertyUtils.TryZeroFloatPropertyDirect(material, "_EmissiveIntensity");

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
            _logger.LogWarning($"{ModInfo.LOG_PREFIX} Terrain foliage suppression failed on terrain '{terrain.name}': {ex.Message}");
            return false;
        }

        return changed;
    }

    private bool TryDarkenTerrainSplatPropertyBlock(Terrain terrain, Material? terrainMaterial, string sceneName)
    {
        try
        {
            MaterialPropertyBlock block = new();
            bool changed = DarkenTerrainSplatPropertyBlock(terrainMaterial, block);
            if (!changed)
            {
                return false;
            }

            terrain.SetSplatMaterialPropertyBlock(block);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"{ModInfo.LOG_PREFIX} Terrain splat darken failed on terrain '{terrain.name}' scene='{sceneName}': {ex.Message}");
            return false;
        }
    }

    private static bool DarkenTerrainSplatPropertyBlock(Material? material, MaterialPropertyBlock block)
    {
        bool changed = false;

        if (material != null)
        {
            changed |= MaterialPropertyUtils.TrySetColorProperty(material, block, "_EmissionColor", Color.black);
            changed |= MaterialPropertyUtils.TrySetTextureProperty(material, block, "_EmissionMap", Texture2D.blackTexture);
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissionStrength");
            changed |= MaterialPropertyUtils.TryZeroFloatProperty(material, block, "_EmissiveIntensity");
        }

        return changed;
    }
}

