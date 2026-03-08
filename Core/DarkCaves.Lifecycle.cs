using UnityEngine.SceneManagement;

namespace DarkCaves;

public sealed partial class DarkCaves
{
    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _coordinator?.Stop();
        if (ReferenceEquals(Instance, this))
        {
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_coordinator == null)
        {
            return;
        }

        _coordinator.QueueSceneStrip(scene, $"scene load ({mode})");
    }

    internal void QueuePostLoadImmediateStrip()
    {
        if (!CanRunAutomaticStrip())
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        Logger.LogInfo($"{ModInfo.LOG_PREFIX} Save load completed; queuing immediate darkness pass for scene '{scene.name}'.");
        _coordinator!.QueueSceneStrip(scene, "save load complete", forceRescan: true, singlePass: true);
    }

    private bool CanRunAutomaticStrip()
    {
        return _coordinator != null;
    }
}
