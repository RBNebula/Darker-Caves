using System;
using DarkCaves.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkCaves.Core;

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

                if (ShouldPreserveLightForWaterCaustics(light))
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

    private static bool ShouldPreserveLightForWaterCaustics(Light light)
    {
        if (HasComponentTypeName(light.gameObject, "CookieFlipbook"))
        {
            return true;
        }

        if (light.cookie == null)
        {
            return false;
        }

        if (ContainsWaterToken(light.name))
        {
            return true;
        }

        Transform? current = light.transform;
        while (current != null)
        {
            if (ContainsWaterToken(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool HasComponentTypeName(GameObject gameObject, string typeNameContains)
    {
        Component[] components = gameObject.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
            {
                continue;
            }

            Type type = component.GetType();
            string fullName = type.FullName ?? type.Name;
            if (fullName.IndexOf(typeNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
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
}

