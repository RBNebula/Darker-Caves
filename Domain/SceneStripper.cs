using BepInEx.Logging;

namespace DarkCaves.Domain;

internal sealed partial class SceneStripper
{
    private static readonly string[] _dustTokens =
    {
        "dustmote",
        "dust_mote",
        "dust mote",
        "particleslight",
        "particle light",
        "dustparticle",
        "dust particle",
    };

    private readonly ManualLogSource _logger;

    public SceneStripper(ManualLogSource logger)
    {
        _logger = logger;
    }
}
