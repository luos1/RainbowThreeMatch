using NUnit.Framework;
using UnityEngine;

public class TabletFocusFireTests
{
    [TestCase(5, 1f)]
    [TestCase(6, 1.5f)]
    public void LargeGroupsHitHarderAndEveryTileStillFiresOnce(int tiles, float firstHitDamage)
    {
        var combat = new TabletCombat();
        combat.ApplyMatch(1, tiles);
        for (int frame = 0; frame < 60 && combat.HitShots == 0; frame++)
            combat.Advance(1.0 / 60);
        Assert.That(combat.HitShots, Is.EqualTo(1));
        Assert.That(combat.TotalDamage, Is.EqualTo(firstHitDamage).Within(.001f));
        combat.Advance(1.2);
        Assert.That(combat.FiredShots, Is.EqualTo(tiles));
        Assert.That(combat.PendingShots, Is.Zero);
    }

    [Test]
    public void FocusFireTightensTheFirstYellowShotsAim()
    {
        var standard = new TabletCombat();
        var focused = new TabletCombat();
        standard.Advance(1.6);
        focused.Advance(1.6);
        standard.ApplyMatch(3, 3);
        focused.ApplyMatch(3, 6);

        standard.Advance(.02);
        focused.Advance(.02);

        Assert.That(standard.FiredShots, Is.EqualTo(1));
        Assert.That(focused.FiredShots, Is.EqualTo(1));
        Assert.That(focused.Shots[0].Target, Is.EqualTo(standard.Shots[0].Target));
        Assert.That(Vector2.Angle(standard.Shots[0].Direction, focused.Shots[0].Direction),
            Is.EqualTo(8f).Within(.1f));
    }

    [Test]
    public void DefenseMatchesBuildTheStreakWithoutQueuingShots()
    {
        var combat = new TabletCombat();
        combat.Advance(4.2);
        combat.ApplyMatch(4, 6);
        combat.ApplyMatch(4, 3);
        combat.ApplyMatch(6, 6); // Still locked.
        bool focused = combat.ApplyMatch(1, 3);

        Assert.That(focused, Is.True);
        Assert.That(combat.OfferedShots, Is.EqualTo(3));
        Assert.That(combat.Shield, Is.Zero);
    }

    [Test]
    public void ANewRoundClearsTheStreakAndQueuedVolley()
    {
        var combat = new TabletCombat();
        combat.Advance(1.6);
        combat.ApplyMatch(1, 6);
        combat.ApplyMatch(2, 3);
        combat.ApplyMatch(3, 3);
        combat.Reset();

        combat.Advance(1.6);
        bool focused = combat.ApplyMatch(1, 3);
        combat.Advance(.121);

        Assert.That(focused, Is.False);
        Assert.That(combat.OfferedShots, Is.EqualTo(3));
        Assert.That(combat.FiredShots, Is.EqualTo(2));
    }
}
