using NUnit.Framework;
using UnityEngine;

public class TabletWeaponFeedbackTests
{
    [TestCase(0, "laserSmall_000", false)]
    [TestCase(1, "laserSmall_003", true)]
    [TestCase(2, "laserRetro_000", true)]
    [TestCase(3, "laserLarge_000", true)]
    public void EachLevelUsesItsOwnImportedShotCue(int level, string clipName, bool upgraded)
    {
        var feedback = new TabletWeaponFeedback(level);
        var clip = Resources.Load<AudioClip>(feedback.ShotClipPath);
        Assert.That(clip, Is.Not.Null);
        Assert.That(clip.name, Is.EqualTo(clipName));
        Assert.That(feedback.Upgraded, Is.EqualTo(upgraded));
        Assert.That(feedback.Volume, Is.InRange(.17f, .25f));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void EachUpgradeChangesTheCoilAndDeepensTheCueAndDefeatEffect(int level)
    {
        var previous = new TabletWeaponFeedback(level - 1);
        var upgraded = new TabletWeaponFeedback(level);
        Assert.That(upgraded.Glow, Is.Not.EqualTo(previous.Glow));
        Assert.That(upgraded.CollarWidth, Is.GreaterThan(previous.CollarWidth));
        Assert.That(upgraded.FlashSize, Is.GreaterThan(previous.FlashSize));
        Assert.That(upgraded.DefeatScale, Is.GreaterThan(previous.DefeatScale));
        Assert.That(upgraded.Pitch, Is.LessThan(previous.Pitch));
    }
}
