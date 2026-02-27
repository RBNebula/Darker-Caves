using System;
using UnityEngine;

namespace DarkCaves.Utilities;

internal static class KeywordMatcher
{
    public static bool HasKeywordInHierarchy(Transform transform, string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return false;
        }

        string[] tokens = csv.Split(',');
        Transform? current = transform;
        while (current != null)
        {
            string currentName = current.name ?? string.Empty;
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i].Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                if (currentName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            current = current.parent;
        }

        return false;
    }

    public static bool ContainsCsvToken(string value, string csv)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(csv))
        {
            return false;
        }

        string[] tokens = csv.Split(',');
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (token.Length == 0)
            {
                continue;
            }

            if (value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsAny(string value, params string[] needles)
    {
        for (int i = 0; i < needles.Length; i++)
        {
            if (value.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
