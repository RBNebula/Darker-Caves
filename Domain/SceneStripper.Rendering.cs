using DarkCaves.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DarkCaves.Domain;

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
                if (behaviour == null || !behaviour.enabled || IsExcludedFromTargeting(behaviour.transform))
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

                if (renderer.reflectionProbeUsage != ReflectionProbeUsage.Off)
                {
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    stats.ReflectionProbeUsageDisabled++;
                }
            }
        }

        return stats;
    }
}
