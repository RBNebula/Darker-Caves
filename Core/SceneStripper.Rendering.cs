using DarkCaves.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System;

namespace DarkCaves.Core;

internal sealed partial class SceneStripper
{
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
                if (behaviour == null || !behaviour.enabled || IsExcludedFromTargeting(behaviour.transform))
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

    public RendererStripStats StripRendererLightingInScene(Scene scene)
    {
        RendererStripStats stats = default;
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
                if (renderer == null || IsExcludedFromTargeting(renderer.transform))
                {
                    continue;
                }

                if (ShouldDisablePlaneRenderer(renderer.transform))
                {
                    if (renderer.enabled)
                    {
                        renderer.enabled = false;
                        stats.PlaneRenderersDisabled++;
                    }

                    continue;
                }

                if (renderer.lightmapIndex >= 0)
                {
                    renderer.lightmapIndex = -1;
                    stats.LightmapCleared++;
                }

                if (renderer.realtimeLightmapIndex >= 0)
                {
                    renderer.realtimeLightmapIndex = -1;
                    stats.LightmapCleared++;
                }

                if (renderer.lightProbeUsage != LightProbeUsage.Off)
                {
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    stats.LightProbeUsageDisabled++;
                }
            }
        }

        return stats;
    }

    private static bool ShouldDisablePlaneRenderer(Transform? transform)
    {
        Transform? current = transform;
        while (current != null)
        {
            if (IsPlaneName(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool IsPlaneName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string name = value!.Trim();
        if (name.Equals("Plane", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (name.StartsWith("Plane ", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Plane(", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Plane_", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Plane-", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}

