using BepInEx;
using DarkCaves.Configuration;
using DarkCaves.Domain;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace DarkCaves;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public sealed class DarkCaves : BaseUnityPlugin
{
    internal static DarkCaves? Instance { get; private set; }

    private SceneStripCoordinator? _coordinator;
    private Harmony? _harmony;

    private void Awake()
    {
        Instance = this;
        DarkCavesConfig config = new();
        SceneStripper stripper = new(Logger);
        _coordinator = new SceneStripCoordinator(this, Logger, config, stripper);
        _harmony = new Harmony(ModInfo.HARMONY_ID);
        _harmony.PatchAll(typeof(DarkCaves).Assembly);

        SceneManager.sceneLoaded += OnSceneLoaded;
        _coordinator.QueueSceneStrip(SceneManager.GetActiveScene(), "awake");
    }

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
        Logger.LogInfo($"Save load completed; queuing immediate darkness pass for scene '{scene.name}'.");
        _coordinator!.QueueSceneStrip(scene, "save load complete", forceRescan: true, singlePass: true);
    }

    private bool CanRunAutomaticStrip()
    {
        return _coordinator != null;
    }
}
