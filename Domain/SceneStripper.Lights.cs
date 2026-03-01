using System;
using DarkCaves.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkCaves.Domain;

internal sealed partial class SceneStripper
{
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
                if (light == null || IsExcludedFromTargeting(light.transform))
                {
                    continue;
                }

                stats.Matched++;
                stats.DriverBehavioursDisabled += DisableLightDriverBehaviours(light.gameObject);
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

        return stats;
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
                if (probe == null || IsExcludedFromTargeting(probe.transform))
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
}
