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
    private static readonly string[] ExpectedTracks =
    [
        "FlipFlap",
        "Tintin",
        "Thunderdome",
        "Constellations",
        "Drifting",
        "starlight",
    ];

    [Test]
    [TestOf(typeof(JukeboxPrototype))]
    public async Task CatalogContainsUpstreamTracks()
    {
        var protoMan = Server.ProtoMan;
        var resources = Server.ResolveDependency<IResourceManager>();

        await Server.WaitAssertion(() =>
        {
            var tracks = protoMan.EnumeratePrototypes<JukeboxPrototype>().ToDictionary(p => p.ID);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tracks.Keys, Is.EquivalentTo(ExpectedTracks),
                    "Jukebox catalog should match upstream Standard.yml track IDs.");

                foreach (var id in ExpectedTracks)
                {
                    Assert.That(tracks[id].Name, Is.Not.Null.And.Not.Empty, $"{id} needs a display name.");
                    Assert.That(tracks[id].Path.Path.ToString(), Does.StartWith("/Audio/Jukebox/"),
                        $"{id} should point at /Audio/Jukebox/.");
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

            system.SetSelectedTrack(ent.AsNullable(), "FlipFlap");
            Assert.That(ent.Comp.SelectedSongId, Is.EqualTo(new ProtoId<JukeboxPrototype>("FlipFlap")));

            Assert.That(system.TryPlay(ent.AsNullable()), Is.True);
            Assert.That(system.IsPlaying(ent.AsNullable()), Is.True);
            Assert.That(ent.Comp.AudioStream, Is.Not.Null);

            system.Stop(ent.AsNullable());
            Assert.That(system.IsPlaying(ent.AsNullable()), Is.False);
        });
    }
}
