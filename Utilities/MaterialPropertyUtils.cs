using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace DarkCaves.Utilities;

internal static class MaterialPropertyUtils
{
    public static bool TryZeroFloatProperty(Material material, MaterialPropertyBlock block, string propertyName)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        float value = material.GetFloat(propertyName);
        block.SetFloat(propertyName, 0f);
        return Math.Abs(value) > 0.0001f;
    }

    public static bool TrySetColorProperty(Material material, MaterialPropertyBlock block, string propertyName, Color target)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        Color current = material.GetColor(propertyName);
        block.SetColor(propertyName, target);
        return !IsSameColor(current, target);
    }

    public static bool TryScaleColorProperty(Material material, MaterialPropertyBlock block, string propertyName, float multiplier)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        Color current = material.GetColor(propertyName);
        Color target = new(current.r * multiplier, current.g * multiplier, current.b * multiplier, current.a);
        block.SetColor(propertyName, target);
        return !IsSameColor(current, target);
    }

    public static bool TrySetTextureProperty(Material material, MaterialPropertyBlock block, string propertyName, Texture texture)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        Texture current = material.GetTexture(propertyName);
        block.SetTexture(propertyName, texture);
        return current != texture;
    }

    public static bool TrySetColorPropertyDirect(Material material, string propertyName, Color target)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        Color current = material.GetColor(propertyName);
        material.SetColor(propertyName, target);
        return !IsSameColor(current, target);
    }

    public static bool TryScaleColorPropertyDirect(Material material, string propertyName, float multiplier)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        Color current = material.GetColor(propertyName);
        Color target = new(current.r * multiplier, current.g * multiplier, current.b * multiplier, current.a);
        material.SetColor(propertyName, target);
        return !IsSameColor(current, target);
    }

    public static bool TryZeroFloatPropertyDirect(Material material, string propertyName)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        float value = material.GetFloat(propertyName);
        material.SetFloat(propertyName, 0f);
        return Math.Abs(value) > 0.0001f;
    }

    public static bool TrySetTexturePropertyDirect(Material material, string propertyName, Texture texture)
    {
        if (!material.HasProperty(propertyName))
        {
            return false;
        }

        Texture current = material.GetTexture(propertyName);
        material.SetTexture(propertyName, texture);
        return current != texture;
    }

    public static bool TryForceDarkenByShaderHeuristics(Material material, bool forceBlackTextures)
    {
        Shader? shader = material.shader;
        if (shader == null)
        {
            return false;
        }

        int propertyCount;
        try
        {
            propertyCount = shader.GetPropertyCount();
        }
        catch
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < propertyCount; i++)
        {
            string propertyName;
            ShaderPropertyType propertyType;
            try
            {
                propertyName = shader.GetPropertyName(i);
                propertyType = shader.GetPropertyType(i);
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                continue;
            }

            if (propertyType == ShaderPropertyType.Color)
            {
                if (LooksLikeColorOrAlbedoProperty(propertyName))
                {
                    changed |= TrySetColorPropertyDirect(material, propertyName, Color.black);
                }
                continue;
            }

            if (propertyType == ShaderPropertyType.Float || propertyType == ShaderPropertyType.Range)
            {
                if (LooksLikeBrightnessOrSpecProperty(propertyName))
                {
                    changed |= TryZeroFloatPropertyDirect(material, propertyName);
                }
                continue;
            }

            if (propertyType == ShaderPropertyType.Texture &&
                forceBlackTextures &&
                LooksLikeAlbedoTextureProperty(propertyName) &&
                !LooksLikeNormalTextureProperty(propertyName))
            {
                changed |= TrySetTexturePropertyDirect(material, propertyName, Texture2D.blackTexture);
            }
        }

        return changed;
    }

    public static bool TryForceDarkenByShaderHeuristics(Material material, MaterialPropertyBlock block, bool forceBlackTextures)
    {
        Shader? shader = material.shader;
        if (shader == null)
        {
            return false;
        }

        int propertyCount;
        try
        {
            propertyCount = shader.GetPropertyCount();
        }
        catch
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < propertyCount; i++)
        {
            string propertyName;
            ShaderPropertyType propertyType;
            try
            {
                propertyName = shader.GetPropertyName(i);
                propertyType = shader.GetPropertyType(i);
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                continue;
            }

            if (propertyType == ShaderPropertyType.Color)
            {
                if (LooksLikeColorOrAlbedoProperty(propertyName))
                {
                    changed |= TrySetColorProperty(material, block, propertyName, Color.black);
                }

                continue;
            }

            if (propertyType == ShaderPropertyType.Float || propertyType == ShaderPropertyType.Range)
            {
                if (LooksLikeBrightnessOrSpecProperty(propertyName))
                {
                    changed |= TryZeroFloatProperty(material, block, propertyName);
                }

                continue;
            }

            if (propertyType == ShaderPropertyType.Texture &&
                forceBlackTextures &&
                LooksLikeAlbedoTextureProperty(propertyName) &&
                !LooksLikeNormalTextureProperty(propertyName))
            {
                changed |= TrySetTextureProperty(material, block, propertyName, Texture2D.blackTexture);
            }
        }

        return changed;
    }

    public static bool IsNearBlack(Color c)
    {
        return c.r <= 0.0001f && c.g <= 0.0001f && c.b <= 0.0001f;
    }

    private static bool LooksLikeColorOrAlbedoProperty(string propertyName)
    {
        return KeywordMatcher.ContainsAny(propertyName, "color", "tint", "albedo", "base", "diffuse", "ground", "layer", "ambient", "detail");
    }

    private static bool LooksLikeBrightnessOrSpecProperty(string propertyName)
    {
        return KeywordMatcher.ContainsAny(
            propertyName,
            "emiss",
            "glow",
            "bright",
            "intens",
            "spec",
            "smooth",
            "metal",
            "reflect",
            "rim",
            "fresnel",
            "ambient",
            "exposure",
            "occlusion",
            "ao",
            "boost",
            "strength",
            "detail");
    }

    private static bool LooksLikeAlbedoTextureProperty(string propertyName)
    {
        return KeywordMatcher.ContainsAny(
            propertyName,
            "main",
            "base",
            "albedo",
            "diffuse",
            "ground",
            "layer",
            "splat",
            "control",
            "mask",
            "terrain",
            "color",
            "detail",
            "map",
            "tex");
    }

    private static bool LooksLikeNormalTextureProperty(string propertyName)
    {
        return KeywordMatcher.ContainsAny(propertyName, "normal", "bump");
    }

    private static bool IsSameColor(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) <= 0.0001f &&
               Mathf.Abs(a.g - b.g) <= 0.0001f &&
               Mathf.Abs(a.b - b.b) <= 0.0001f &&
               Mathf.Abs(a.a - b.a) <= 0.0001f;
    }
}
