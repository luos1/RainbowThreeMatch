using System;
using NUnit.Framework;
using Rainbow.Services;
using UnityEngine;

public class CloudSaveServiceTests
{
    private const string Uid = "cloud-save-test-user";
    private LocalCloudSaveService local;

    [SetUp]
    public void SetUp()
    {
        local = new LocalCloudSaveService("RainbowThreeMatch.Tests.CloudSave");
        local.Delete(Uid);
    }

    [TearDown]
    public void TearDown() => local.Delete(Uid);

    [Test]
    public void ConflictResolutionIsCommutativeAndKeepsHighestScoreAndNewestMetadata()
    {
        var olderHighScore = new SaveData(300, 1000, "Older");
        var newerMetadata = new SaveData(100, 2000, "Newer");

        SaveData left = SaveConflict.Resolve(olderHighScore, newerMetadata);
        SaveData right = SaveConflict.Resolve(newerMetadata, olderHighScore);

        Assert.That(left.HighScore, Is.EqualTo(300));
        Assert.That(left.UpdatedAt, Is.EqualTo(2000));
        Assert.That(left.DisplayName, Is.EqualTo("Newer"));
        Assert.That(right, Is.EqualTo(left));
        Assert.That(left, Is.Not.SameAs(olderHighScore));
        Assert.That(left, Is.Not.SameAs(newerMetadata));
    }

    [Test]
    public void EqualTimestampUsesOrdinalMaximumNameForDeterministicMerge()
    {
        var first = new SaveData(10, 1000, "Alpha");
        var second = new SaveData(20, 1000, "Zulu");

        SaveData merged = SaveConflict.Resolve(first, second);

        Assert.That(merged.HighScore, Is.EqualTo(20));
        Assert.That(merged.DisplayName, Is.EqualTo("Zulu"));
        Assert.That(SaveConflict.Resolve(second, first), Is.EqualTo(merged));
        Assert.That(SaveConflict.Resolve(merged, merged), Is.EqualTo(merged));
    }

    [Test]
    public void SaveDataRejectsANegativeHighScore()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SaveData(-1, 0, "Player"));
    }

    [Test]
    public void SaveDataRejectsANegativeTimestamp()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SaveData(0, -1, "Player"));
    }

    [Test]
    public void SaveDataRejectsAnOverlongDisplayName()
    {
        Assert.Throws<ArgumentException>(() => new SaveData(0, 0, new string('x', 81)));
    }

    [Test]
    public void InMemoryLoadReturnsNullForAnUnknownUser()
    {
        var store = new InMemoryCloudSaveService();

        Assert.That(store.Load(Uid).GetAwaiter().GetResult(), Is.Null);
    }

    [Test]
    public void InMemorySaveMergesConflictingSnapshots()
    {
        var store = new InMemoryCloudSaveService();

        store.Save(Uid, new SaveData(50, 2000, "Newest")).GetAwaiter().GetResult();
        SaveData second = store.Save(Uid, new SaveData(100, 1000, "Older")).GetAwaiter().GetResult();

        Assert.That(second, Is.EqualTo(new SaveData(100, 2000, "Newest")));
    }

    [Test]
    public void InMemoryStoreReturnsDefensiveCopies()
    {
        var store = new InMemoryCloudSaveService();
        SaveData saved = store.Save(Uid, new SaveData(50, 2000, "Original")).GetAwaiter().GetResult();

        saved.DisplayName = "Mutated caller copy";
        SaveData loaded = store.Load(Uid).GetAwaiter().GetResult();

        Assert.That(loaded.DisplayName, Is.EqualTo("Original"));
        Assert.That(loaded, Is.Not.SameAs(saved));
    }

    [Test]
    public void InMemoryStoreKeepsUsersIsolated()
    {
        var store = new InMemoryCloudSaveService();
        store.Save("first-user", new SaveData(10, 1000, "First")).GetAwaiter().GetResult();
        store.Save("second-user", new SaveData(20, 2000, "Second")).GetAwaiter().GetResult();

        Assert.That(store.Load("first-user").GetAwaiter().GetResult(),
            Is.EqualTo(new SaveData(10, 1000, "First")));
        Assert.That(store.Load("second-user").GetAwaiter().GetResult(),
            Is.EqualTo(new SaveData(20, 2000, "Second")));
    }

    [Test]
    public void LocalSavePersistsResolvedSnapshot()
    {
        local.Save(Uid, new SaveData(25, 2000, "Newest")).GetAwaiter().GetResult();
        local.Save(Uid, new SaveData(50, 1000, "Older")).GetAwaiter().GetResult();

        var reloaded = new LocalCloudSaveService("RainbowThreeMatch.Tests.CloudSave");
        Assert.That(reloaded.Load(Uid).GetAwaiter().GetResult(),
            Is.EqualTo(new SaveData(50, 2000, "Newest")));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void InMemoryStoreRejectsBlankUserIds(string uid)
    {
        var store = new InMemoryCloudSaveService();

        Assert.Throws<ArgumentException>(() => store.Load(uid).GetAwaiter().GetResult());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void LocalStoreRejectsBlankUserIds(string uid)
    {
        Assert.Throws<ArgumentException>(() => local.Save(uid, new SaveData()).GetAwaiter().GetResult());
    }
}
