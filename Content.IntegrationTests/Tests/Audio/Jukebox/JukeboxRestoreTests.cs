#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Audio.Jukebox;
using Content.Shared.Audio.Jukebox;
using Content.Shared._Sunrise.TapePlayer;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Audio.Jukebox;

/// <summary>
/// Ensures the upstream SS14 jukebox catalog and machine wiring remain restored
/// (not replaced by Sunrise TapePlayer on the station jukebox).
/// </summary>
public sealed class JukeboxRestoreTests : GameTest
{
    /// <summary>Original upstream SS14 jukebox tracks.</summary>
    private static readonly string[] UpstreamTracks =
    [
        "FlipFlap",
        "Tintin",
        "Thunderdome",
        "Constellations",
        "Drifting",
        "starlight",
    ];

    /// <summary>Licensed classic SS13 title/lobby + Fish tracks added to the jukebox catalog.</summary>
    private static readonly string[] ClassicSs13AndFishTracks =
    [
        "EndlessSpace",
        "Absconditus",
        "RobocopTitle",
        "AtomicAmnesia",
        "Singuloose",
        "CometHaley",
        "SpacStac",
        "PhoronWillMakeUsRich",
        "LasersRipApartTheBulkhead",
        "EveryLightIsBlinkingAtOnce",
        "SpaceAsshole",
        "TheWizard",
        "Sunset",
        "Neon",
        "DoubleU",
    ];

    /// <summary>Explicit SS13/SS14 community soundtrack artists already licensed in-repo.</summary>
    private static readonly string[] CommunitySsSoundtrackTracks =
    [
        "BolgarichBurnTheHive",
        "BolgarichDestinyOfColonists",
        "BolgarichDestroyed",
        "BolgarichHikingInTheFog",
        "BolgarichTangoWithBloodshed",
        "BolgarichTearOfVeil",
        "BolgarichTheirFinalShift",
        "BolgarichVendetta",
        "BolgarichRunningOut",
        "Countdown",
        "SoundStation14",
        "BlatantlyNuclear",
        "ClearlyNuclear",
        "NukeMass",
        "LastCall",
        "SponsoredBySyndicate",
        "NuclearOpsFull",
        "NuclearOpsPrep",
        "NuclearOpsInfil",
        "NuclearOpsDef",
        "RevolutionTheme",
        "Enigma",
        "CodeSierra",
    ];

    [Test]
    [TestOf(typeof(JukeboxPrototype))]
    public async Task CatalogContainsLicensedTracks()
    {
        var protoMan = Server.ProtoMan;
        var resources = Server.ResolveDependency<IResourceManager>();
        var required = UpstreamTracks
            .Concat(ClassicSs13AndFishTracks)
            .Concat(CommunitySsSoundtrackTracks)
            .ToArray();

        await Server.WaitAssertion(() =>
        {
            var tracks = protoMan.EnumeratePrototypes<JukeboxPrototype>().ToDictionary(p => p.ID);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tracks.Keys, Is.SupersetOf(required),
                    "Jukebox catalog is missing required licensed tracks.");

                foreach (var id in required)
                {
                    Assert.That(tracks[id].Name, Is.Not.Null.And.Not.Empty, $"{id} needs a display name.");
                    Assert.That(resources.ContentFileExists(tracks[id].Path.Path), Is.True,
                        $"Missing audio resource for {id}: {tracks[id].Path.Path}");
                }
            }
        });
    }

    [Test]
    [TestOf(typeof(JukeboxComponent))]
    public async Task JukeboxEntityUsesOriginalPlayerNotTapePlayer()
    {
        var jukebox = await Spawn("Jukebox");

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(SEntMan.HasComponent<JukeboxComponent>(jukebox), Is.True,
                    "Jukebox entity must use JukeboxComponent.");
                Assert.That(SEntMan.HasComponent<TapePlayerComponent>(jukebox), Is.False,
                    "Station jukebox must not use Sunrise TapePlayer.");
            }
        });
    }

    [Test]
    [TestOf(typeof(JukeboxSystem))]
    public async Task SelectPlayAndStopTrack()
    {
        var jukebox = await Spawn("Jukebox");

        await Server.WaitAssertion(() =>
        {
            var system = SEntMan.System<JukeboxSystem>();
            var ent = SEntity<JukeboxComponent>(jukebox);

            system.SetSelectedTrack(ent.AsNullable(), "EndlessSpace");
            Assert.That(ent.Comp.SelectedSongId, Is.EqualTo(new ProtoId<JukeboxPrototype>("EndlessSpace")));

            Assert.That(system.TryPlay(ent.AsNullable()), Is.True);
            Assert.That(system.IsPlaying(ent.AsNullable()), Is.True);
            Assert.That(ent.Comp.AudioStream, Is.Not.Null);

            system.Stop(ent.AsNullable());
            Assert.That(system.IsPlaying(ent.AsNullable()), Is.False);
        });
    }
}
