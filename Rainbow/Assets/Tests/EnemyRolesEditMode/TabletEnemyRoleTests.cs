using System;
using NUnit.Framework;

public class TabletEnemyRoleTests
{
    const int FirstGunner = 10;

    [Test]
    public void GunnerWarnsEarlierThanScoutAndBecomesTheAttackTarget()
    {
        var combat = new TabletCombat();
        AdvanceTo(combat, 18.8);

        Assert.That(combat.Enemies[FirstGunner].Warning, Is.True);
        Assert.That(combat.Enemies[FirstGunner + 1].Warning, Is.False);
        Assert.That(combat.TargetIndex, Is.EqualTo(FirstGunner));
    }

    [Test]
    public void OrangeVolleyDestroysTheChargingGunnerAndCancelsItsShot()
    {
        var combat = new TabletCombat();
        AdvanceTo(combat, 18.8);
        int enemyShots = combat.FiredHostiles;
        combat.ApplyMatch(2, 3);
        combat.Advance(20.4 - combat.Elapsed);

        Assert.That(combat.Enemies[FirstGunner].Hp, Is.Zero);
        Assert.That(combat.Enemies[FirstGunner].Warning, Is.False);
        Assert.That(combat.OfferedShots, Is.EqualTo(3));
        Assert.That(combat.FiredShots, Is.EqualTo(3));
        Assert.That(combat.FiredHostiles, Is.EqualTo(enemyShots));
    }

    [Test]
    public void AHeavyShellCostsTwoHullAndGreenRepairsIt()
    {
        var combat = new TabletCombat();
        AdvanceTo(combat, 19.9);
        combat.ApplyMatch(4, 3);
        int contacts = combat.HullContacts;

        combat.Advance(1.8);
        Assert.That(combat.HullContacts - contacts, Is.EqualTo(1));
        Assert.That(combat.Hp, Is.EqualTo(combat.Hull - 2));

        combat.ApplyMatch(4, 3);
        Assert.That(combat.Hp, Is.EqualTo(combat.Hull));
        Assert.That(combat.OfferedShots, Is.Zero);
    }

    [Test]
    public void BlueBlocksTheHeavyShellBeforeItReachesTheHull()
    {
        var combat = new TabletCombat();
        AdvanceTo(combat, 40.05);
        combat.ApplyMatch(4, 3);
        combat.ApplyMatch(6, 3);
        int health = combat.Hp;

        combat.Advance(1.5);
        Assert.That(combat.Hp, Is.EqualTo(health));
        Assert.That(combat.BlockedDamage, Is.EqualTo(1));
        Assert.That(combat.OfferedShots, Is.Zero);
    }

    static void AdvanceTo(TabletCombat combat, double time)
    {
        while (combat.Elapsed + .001 < time && combat.Result == TabletResult.Playing)
        {
            if (combat.Hp < combat.Hull) combat.ApplyMatch(4, 3);
            combat.Advance(Math.Min(.25, time - combat.Elapsed));
        }
        Assert.That(combat.Elapsed, Is.EqualTo(time).Within(.001));
    }
}
