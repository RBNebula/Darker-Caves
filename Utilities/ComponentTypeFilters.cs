using System;

namespace DarkCaves.Utilities;

internal static class ComponentTypeFilters
{
    public static bool IsPostProcessingComponent(string fullName)
    {
        if (fullName.IndexOf("PostProcess", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (fullName.Equals("UnityEngine.Rendering.Volume", StringComparison.Ordinal))
        {
            return true;
        }

        if (fullName.IndexOf("Rendering.Universal.Volume", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    public static bool IsProjectorLikeComponent(string fullName)
    {
        if (fullName.IndexOf("Projectile", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        if (fullName.IndexOf("Projector", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (fullName.IndexOf("Decal", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    public static bool IsLikelyLightDriver(Type type)
    {
        string fullName = type.FullName ?? type.Name;
        return fullName.IndexOf("CookieFlipbook", StringComparison.OrdinalIgnoreCase) >= 0 ||
               fullName.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
