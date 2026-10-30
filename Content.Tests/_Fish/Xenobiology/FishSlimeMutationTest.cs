using Content.Shared._Fish.Xenobiology.Slimes;
using NUnit.Framework;

namespace Content.Tests._Fish.Xenobiology;

[TestFixture]
public sealed class FishSlimeMutationTest
{
    [Test]
    public void GuaranteedMutationIsRainbow()
    {
        Assert.That(FishSlimeMutation.RollChild("grey", 100, 0, 0), Is.EqualTo("rainbow"));
        Assert.That(FishSlimeMutation.RollChild("black", 100, 99, 3), Is.EqualTo("rainbow"));
    }

    [Test]
    public void GreyUsesAllFourTierTwoColors()
    {
        Assert.That(FishSlimeMutation.RollChild("grey", 30, 0, 0), Is.EqualTo("orange"));
        Assert.That(FishSlimeMutation.RollChild("grey", 30, 0, 1), Is.EqualTo("metal"));
        Assert.That(FishSlimeMutation.RollChild("grey", 30, 0, 2), Is.EqualTo("blue"));
        Assert.That(FishSlimeMutation.RollChild("grey", 30, 0, 3), Is.EqualTo("purple"));
    }

    [Test]
    public void FailedRollKeepsParentColor()
    {
        Assert.That(FishSlimeMutation.RollChild("grey", 30, 30, 0), Is.EqualTo("grey"));
        Assert.That(FishSlimeMutation.RollChild("grey", 30, 99, 3), Is.EqualTo("grey"));
    }

    [Test]
    public void RepeatedSlotsDoubleTheWeight()
    {
        var slots = FishSlimeMutation.Slots("purple");
        Assert.That(slots[2], Is.EqualTo("green"));
        Assert.That(slots[3], Is.EqualTo("green"));
        Assert.That(FishSlimeMutation.RollChild("red", 40, 0, 2), Is.EqualTo("oil"));
        Assert.That(FishSlimeMutation.RollChild("red", 40, 0, 0), Is.EqualTo("red"));
    }

    [Test]
    public void TierFiveMutatesIntoItself()
    {
        Assert.That(FishSlimeMutation.RollChild("adamantine", 50, 0, 1), Is.EqualTo("adamantine"));
        Assert.That(FishSlimeMutation.RollChild("sepia", 50, 0, 3), Is.EqualTo("sepia"));
        Assert.That(FishSlimeMutation.RollChild("rainbow", 50, 10, 0), Is.EqualTo("rainbow"));
    }

    [Test]
    public void PrototypeIdsMatchColorAndStage()
    {
        Assert.That(FishSlimeMutation.EntityId("grey", false), Is.EqualTo("FishSlimeGreyBabyFish"));
        Assert.That(FishSlimeMutation.EntityId("darkblue", true), Is.EqualTo("FishSlimeDarkBlueAdultFish"));
        Assert.That(FishSlimeMutation.EntityId("lightpink", false), Is.EqualTo("FishSlimeLightPinkBabyFish"));
        Assert.That(FishSlimeMutation.EntityId("nope", true), Is.EqualTo("FishSlimeGreyAdultFish"));
    }

    [Test]
    public void ChanceClampsToPercent()
    {
        Assert.That(FishSlimeMutation.ClampChance(-4), Is.EqualTo(0));
        Assert.That(FishSlimeMutation.ClampChance(140), Is.EqualTo(100));
        Assert.That(FishSlimeMutation.EvolutionThreshold, Is.EqualTo(10));
    }
}
