using HarmonyLib;

namespace DarkCaves.Patches;

[HarmonyPatch(typeof(SavingLoadingManager), nameof(SavingLoadingManager.LoadGame))]
internal static class SavingLoadingManagerLoadGamePatch
{
    [HarmonyPostfix]
    private static void LoadGamePostfix()
    {
        DarkCaves.Instance?.QueuePostLoadImmediateStrip();
    }
}
