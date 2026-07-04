using System.Collections.Generic;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests.Atmos;

/// <summary>
/// Проверяет, что <see cref="AtmosphereSystem.GetTileMixtures"/> корректно обрабатывает
/// каждый тайл отдельно, а не подменяет все смеси при отсутствии одного тайла на гриде.
/// </summary>
[TestOf(typeof(AtmosphereSystem))]
public sealed class GetTileMixturesTest : AtmosTest
{
    protected override ResPath? TestMapPath => new("Maps/Test/Atmospherics/DeltaPressure/deltapressuretest.yml");

    [Test]
    public async Task GetTileMixtures_FallsBackPerTile()
    {
        SAtmos.RunProcessingFull(ProcessEnt, MapData.Grid.Owner, SAtmos.AtmosTickRate);

        await Server.WaitPost(() =>
        {
            var validTile = Vector2i.Zero;
            var missingTile = new Vector2i(9999, 9999);

            var validMixture = SAtmos.GetTileMixture(ProcessEnt.Owner, MapData.MapUid, validTile);
            Assert.That(validMixture, Is.Not.Null, "Expected a gas mixture on the test map center tile.");

            var tiles = new List<Vector2i> { validTile, missingTile };
            var mixtures = SAtmos.GetTileMixtures(ProcessEnt.Owner, MapData.MapUid, tiles);

            Assert.That(mixtures, Has.Length.EqualTo(2));
            Assert.That(mixtures[0], Is.SameAs(validMixture),
                "Valid tile mixture must not be replaced when another tile is missing from the grid.");
            Assert.That(mixtures[1], Is.EqualTo(GasMixture.SpaceGas),
                "Missing grid tile must fall back to space gas, not overwrite other results.");
        });
    }
}
