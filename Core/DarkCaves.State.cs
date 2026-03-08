using HarmonyLib;
using DarkCaves.Core;

namespace DarkCaves;

public sealed partial class DarkCaves
{
    internal static DarkCaves? Instance { get; private set; }

    private SceneStripCoordinator? _coordinator;
    private Harmony? _harmony;
}
