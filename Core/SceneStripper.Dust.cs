using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkCaves.Core;

internal sealed partial class SceneStripper
{
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

            ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int j = 0; j < particleSystems.Length; j++)
            {
                ParticleSystem particleSystem = particleSystems[j];
                if (particleSystem == null || IsExcludedFromTargeting(particleSystem.transform))
                {
                    continue;
                }

                bool dustByName = IsDustTarget(particleSystem.transform, particleSystem.name);
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

            Behaviour[] behaviours = root.GetComponentsInChildren<Behaviour>(true);
            for (int j = 0; j < behaviours.Length; j++)
            {
                Behaviour behaviour = behaviours[j];
                if (behaviour == null || !behaviour.enabled || IsExcludedFromTargeting(behaviour.transform))
                {
                    continue;
                }

                string fullName = behaviour.GetType().FullName ?? behaviour.GetType().Name;
                bool dustByName = IsDustTarget(behaviour.transform, fullName, behaviour.name);
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

        return stats;
    }

    private static bool IsDustTarget(Transform? transform, params string[] values)
    {
        if (ContainsDustTokenInHierarchy(transform))
        {
            return true;
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (ContainsDustToken(values[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsDustTokenInHierarchy(Transform? transform)
    {
        Transform? current = transform;
        while (current != null)
        {
            if (ContainsDustToken(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool ContainsDustToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string source = value!;
        for (int i = 0; i < _dustTokens.Length; i++)
        {
            if (source.IndexOf(_dustTokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}

