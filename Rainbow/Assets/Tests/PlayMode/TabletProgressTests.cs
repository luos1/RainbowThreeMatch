using System;
using NUnit.Framework;
using UnityEngine;

public class TabletProgressTests
{
    string key;

    [SetUp]
    public void SetUp() => key = "Rainbow.TabletProgress.PlayMode." + Guid.NewGuid().ToString("N");

    [TearDown]
    public void TearDown() => PlayerPrefs.DeleteKey(key);

    [Test]
    public void RewardCreditsAreIdempotentAcrossReloadAndOnlyApplyToTheirOwnRun()
    {
        var progress = new TabletProgress(key);
        int first = progress.BeginRun();
        var stale = new TabletProgress(key);
        Assert.That(progress.Credit(first, 1), Is.EqualTo(20));
        Assert.That(stale.Credit(first, 1), Is.Zero);
        progress = new TabletProgress(key);
        Assert.That(progress.Credit(first, 1), Is.Zero);
        Assert.That(progress.Credit(first, 1 | TabletProgress.BossBit), Is.EqualTo(75));
        Assert.That(progress.Credit(first, 14 | 1 << 10), Is.EqualTo(85));
        Assert.That(progress.Balance, Is.EqualTo(180));
        int second = progress.BeginRun();
        Assert.That(progress.Credit(first, 1), Is.Zero);
        Assert.That(progress.Credit(second, 1), Is.EqualTo(20));
        Assert.That(new TabletProgress(key).Balance, Is.EqualTo(200));
    }

    [Test]
    public void PurchasesDebitTheCurrentPriceAndRestoreMasteryAndHullOnReload()
    {
        var progress = new TabletProgress(key);
        Assert.That(progress.Buy(TabletUpgrade.Attack), Is.False);
        for (int run = 0; run < 3; run++) progress.Credit(progress.BeginRun(), 31);
        Assert.That(progress.Buy(TabletUpgrade.Attack), Is.True);
        Assert.That(progress.Price(TabletUpgrade.Attack), Is.EqualTo(75));
        Assert.That(progress.Buy(TabletUpgrade.Attack), Is.True);
        Assert.That(progress.Buy(TabletUpgrade.Attack), Is.True);
        Assert.That(progress.Buy(TabletUpgrade.Attack), Is.False);
        Assert.That(progress.Buy(TabletUpgrade.Hull), Is.True);
        var reloaded = new TabletProgress(key);
        Assert.That(reloaded.Power, Is.EqualTo(1.3f).Within(.001));
        Assert.That(reloaded.MaxHp, Is.EqualTo(11));
        Assert.That(reloaded.Balance, Is.EqualTo(3 * 180 - 40 - 75 - 115 - 35));
    }

    [TestCase("invalid JSON")]
    [TestCase("{\"version\":1,\"balance\":-20,\"attack\":3,\"hull\":3,\"run\":2,\"awardedMask\":1}")]
    [TestCase("{\"version\":1,\"balance\":20,\"attack\":4,\"run\":2,\"awardedMask\":1}")]
    [TestCase("{\"version\":1,\"balance\":20,\"run\":0,\"awardedMask\":1}")]
    public void DamagedSaveFallsBackToAnEmptyPlayableProfile(string raw)
    {
        PlayerPrefs.SetString(key, raw);
        var progress = new TabletProgress(key);
        Assert.That(progress.Balance, Is.Zero);
        Assert.That(progress.MaxHp, Is.EqualTo(10));
        Assert.That(progress.Power, Is.EqualTo(1));
        Assert.That(progress.Credit(progress.BeginRun(), 1), Is.EqualTo(20));
    }
}
