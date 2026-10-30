using Content.Server._Fish.Mining;
using Content.Server.Power.Components;
using Content.Shared._Fish.Mining;
using Content.Shared.Lathe;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Fish.Mining;

[TestFixture]
public sealed class OreRedemptionTest
{
    private static readonly EntProtoId OreProcessorProto = "OreProcessor";
    private static readonly EntProtoId OreProcessorIndustrialProto = "OreProcessorIndustrial";
    private static readonly EntProtoId SteelOreProto = "SteelOre1";
    private static readonly EntProtoId CoalProto = "Coal1";
    private static readonly ProtoId<LatheRecipePrototype> SteelRecipe = "SheetSteel";

    [Test]
    public async Task OreProcessorHasRedemptionComponent()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var factory = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            Assert.That(protoMan.TryIndex(OreProcessorProto, out EntityPrototype processor));
            Assert.That(processor.TryGetComponent<OreRedemptionMachineComponent>(out _, factory));
            Assert.That(processor.TryGetComponent<LatheComponent>(out _, factory));
            Assert.That(processor.TryGetComponent<MaterialStorageComponent>(out _, factory));

            Assert.That(protoMan.TryIndex(OreProcessorIndustrialProto, out EntityPrototype industrial));
            Assert.That(industrial.TryGetComponent<OreRedemptionMachineComponent>(out _, factory));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AbsorbsNearbyOreAndQueuesSmelting()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var mapData = await pair.CreateTestMap();
        var entMan = server.EntMan;
        var materials = server.System<SharedMaterialStorageSystem>();
        var protoMan = server.ProtoMan;

        var processor = EntityUid.Invalid;
        var ore = EntityUid.Invalid;
        var coal = EntityUid.Invalid;
        var absorbedOre = false;
        var absorbedCoal = false;

        await server.WaitPost(() =>
        {
            processor = entMan.SpawnEntity(OreProcessorProto, mapData.GridCoords);
            if (entMan.TryGetComponent(processor, out ApcPowerReceiverComponent power))
            {
                power.NeedsPower = false;
                power.Powered = true;
            }

            ore = entMan.SpawnEntity(SteelOreProto, mapData.GridCoords);

            var redemption = server.System<OreRedemptionSystem>();
            var storage = entMan.GetComponent<MaterialStorageComponent>(processor);
            var redemptionComp = entMan.GetComponent<OreRedemptionMachineComponent>(processor);

            materials.UpdateMaterialWhitelist(processor);

            absorbedOre = redemption.TryAbsorbOre(processor, ore, storage, redemptionComp);

            coal = entMan.SpawnEntity(CoalProto, mapData.GridCoords);
            absorbedCoal = redemption.TryAbsorbOre(processor, coal, storage, redemptionComp);
            redemption.TryAutoProcess(processor);
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(absorbedOre, Is.True, "Direct ore absorb should succeed");
            Assert.That(entMan.Deleted(ore) || entMan.IsQueuedForDeletion(ore), Is.True);
            Assert.That(materials.GetMaterialAmount(processor, "RawIron"), Is.GreaterThan(0));
            Assert.That(absorbedCoal, Is.True);

            var lathe = entMan.GetComponent<LatheComponent>(processor);
            var queuedSteel = lathe.CurrentRecipe == SteelRecipe;
            foreach (var batch in lathe.Queue)
            {
                if (batch.Recipe == SteelRecipe)
                    queuedSteel = true;
            }

            Assert.That(queuedSteel, Is.True,
                "With iron and coal the processor should auto-queue steel smelting");
            Assert.That(protoMan.Index(SteelRecipe).PrintTicket, Is.True,
                "Steel smelting should print a salvage ticket");
        });

        await pair.CleanReturnAsync();
    }
}
