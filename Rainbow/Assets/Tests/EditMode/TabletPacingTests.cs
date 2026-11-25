using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class TabletPacingTests
{
    [Test]
    public void ClearingTheFormationPullsTheNextEntryForwardAndRetryRestoresTheSchedule()
    {
        var combat = new TabletCombat();
        combat.Reset(hull: 100);
        combat.Advance(1.5);
        for (int i = 0; i < 4; i++)
            Hit(combat, i, 2);
        combat.Advance(1);
        Assert.That(combat.Enemies[5].Spawned, Is.False);

        Hit(combat, 4, 1);
        combat.Advance(1.2);
        Assert.That(combat.Enemies[5].Spawned, Is.False, "The transition leaves a brief breather.");
        combat.Advance(.1);
        Assert.That(combat.Enemies[5].Spawned, Is.True);
        Assert.That(combat.Enemies[6].Spawned, Is.False, "Wingmates should still enter in order.");
        combat.Advance(.2);
        Assert.That(combat.Enemies[6].Spawned, Is.True);
        Assert.That(combat.Enemies[5].Warning, Is.False);
        combat.Advance(.8);
        Assert.That(combat.Enemies[5].Warning, Is.True, "Attack preparation moves with entry.");

        combat.Reset();
        combat.Advance(5);
        Assert.That(combat.Enemies[5].Spawned, Is.False);
    }

    [Test]
    public void AQueuedReinforcementSharesTheAcceleratedFormationEntry()
    {
        var combat = new TabletCombat();
        combat.RegisterMiss();
        combat.RegisterMiss();
        Assert.That(combat.RegisterMiss(), Is.EqualTo(TabletMissOutcome.Queued));
        combat.Advance(1.5);
        for (int i = 0; i < 5; i++)
            Hit(combat, i, 2);
        combat.Advance(2.75);
        Assert.That(combat.ReinforcementsArrived, Is.EqualTo(5));
        Assert.That(combat.ReinforcementsPending, Is.Zero);
    }

    [Test]
    public void FastClearKeepsTheApproachCueBeforeAnEarlierBossArrives()
    {
        var combat = new TabletCombat();
        combat.Reset(hull: 100, power: 20);
        combat.ApplyMatch(1, 120);
        while (combat.Elapsed < 30 && combat.BossPhase == TabletBossPhase.None)
            combat.Advance(.05);

        Assert.That(combat.BossPhase, Is.EqualTo(TabletBossPhase.Approaching));
        Assert.That(combat.Boss.Spawned, Is.False);
        Assert.That(combat.EnemiesDefeated, Is.EqualTo(TabletCombat.EnemyCount - 1));
        Assert.That(combat.Wave, Is.EqualTo(3));
        Assert.That(combat.OfferedShots, Is.EqualTo(120));
        combat.Advance(1);
        Assert.That(combat.Boss.Spawned, Is.False);
        combat.Advance(.75);
        Assert.That(combat.Boss.Spawned, Is.True);
    }

    [Test]
    public void BossTelegraphsTheSalvoAndTakesExtraDamageOnlyDuringTheCounterWindow()
    {
        var combat = new TabletCombat();
        combat.Reset(hull: 100);
        int bossIndex = TabletCombat.EnemyCount - 1;
        combat.Advance(46.9);
        Assert.That(combat.BossPhase, Is.EqualTo(TabletBossPhase.Warning));
        Assert.That(combat.Boss.Warning, Is.True);
        Assert.That(combat.Boss.Volleys, Is.Zero);
        Hit(combat, bossIndex, 1);
        Assert.That(combat.Boss.Hp, Is.EqualTo(8));

        combat.Advance(.9);
        Assert.That(combat.BossPhase, Is.EqualTo(TabletBossPhase.Counterattack));
        Assert.That(combat.Boss.Warning, Is.False);
        Assert.That(combat.Hostiles.Count(shot => shot.Active && shot.Boss), Is.EqualTo(3));
        Hit(combat, bossIndex, 1);
        Assert.That(combat.Boss.Hp, Is.EqualTo(6.5));

        combat.Advance(1.8);
        Assert.That(combat.BossPhase, Is.EqualTo(TabletBossPhase.Recovery));
        Assert.That(combat.Hostiles.Any(shot => shot.Active && shot.Boss), Is.False);
        Hit(combat, bossIndex, 1);
        Assert.That(combat.Boss.Hp, Is.EqualTo(5.5));

        combat.Advance(.6);
        Assert.That(combat.BossPhase, Is.EqualTo(TabletBossPhase.Warning));
        combat.Advance(6);
        Assert.That(combat.Boss.Volleys, Is.EqualTo(3));
        Assert.That(combat.BossPhase, Is.EqualTo(TabletBossPhase.Recovery));
        combat.Advance(4);
        Assert.That(combat.BossPhase, Is.EqualTo(TabletBossPhase.None));
        combat.Reset();
        Assert.That(combat.BossPhase, Is.EqualTo(TabletBossPhase.None));
    }

    static void Hit(TabletCombat combat, int index, float damage)
    {
        // Control the impact frame without depending on muzzle aim or flight time.
        var hit = typeof(TabletCombat).GetMethod("HurtEnemy", BindingFlags.Instance | BindingFlags.NonPublic);
        hit.Invoke(combat, new object[] { index, damage, 1, Vector2.up });
    }
}
