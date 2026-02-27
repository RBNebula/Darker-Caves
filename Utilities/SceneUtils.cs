using System;
using UnityEngine.SceneManagement;

namespace DarkCaves.Utilities;

internal static class SceneUtils
{
    public static bool IsIgnoredScene(string sceneName, string csv)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(csv))
        {
            return false;
        }

        string[] items = csv.Split(',');
        for (int i = 0; i < items.Length; i++)
        {
            string item = items[i].Trim();
            if (item.Length == 0)
            {
                continue;
            }

            if (sceneName.Equals(item, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetSceneKey(Scene scene)
    {
        return string.IsNullOrWhiteSpace(scene.path) ? scene.name : scene.path;
    }
}
