using BepInEx;
using DarkCaves.Config;
using DarkCaves.Core;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace DarkCaves;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
public sealed partial class DarkCaves : BaseUnityPlugin
{
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
        Logger.LogInfo($"{ModInfo.LOG_PREFIX} Initialized.");
    }
}
