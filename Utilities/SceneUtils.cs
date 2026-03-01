using System;
using UnityEngine.SceneManagement;

namespace DarkCaves.Utilities;

internal static class SceneUtils
{
    private const string IgnoredSceneName = "MainMenu";

    public static bool IsIgnoredScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
               sceneName.Equals(IgnoredSceneName, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetSceneKey(Scene scene)
    {
        return string.IsNullOrWhiteSpace(scene.path) ? scene.name : scene.path;
    }
}
