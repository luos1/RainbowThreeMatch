using NUnit.Framework;
using UnityEngine;

public class TabletCombatTests
{
    [Test]
    public void AFullOpeningFlightArrivesBeforeAnyDamageAndFirstContactIsDelayedByTravel()
    {
        var combat = new TabletCombat();
        combat.Advance(1.6);
        int visible = 0;
        foreach (var enemy in combat.Enemies)
            if (enemy.Spawned && enemy.Position.x >= 0 && enemy.Position.x <= 1 && enemy.Position.y <= 1)
                visible++;
        Assert.That(visible, Is.EqualTo(5));
        combat.Advance(1.65);
        Assert.That(combat.Hp, Is.EqualTo(10));
        Assert.That(combat.ActiveHostiles, Is.Positive);
        combat.Advance(1.7);
        Assert.That(combat.Hp, Is.LessThan(10));
        Assert.That(combat.HullContacts, Is.Positive);
    }

    [Test]
    public void AProjectileCanMissTheHullOrBeCaughtByTheVisibleShieldRim()
    {
        var from = new Vector2(.274f, .45f);
        var to = new Vector2(.274f, -.1f);
        Assert.That(TabletCombat.IntersectsBow(from, to, false, out _), Is.False);
        Assert.That(TabletCombat.IntersectsBow(from, to, true, out float contact), Is.True);
        Assert.That(contact, Is.InRange(0f, 1f));
        Assert.That(TabletCombat.IntersectsBow(new Vector2(0, -.012f), new Vector2(0, -.52f), true, out _), Is.False);
    }

    [Test]
    public void SimultaneousShellsCannotStackDamageDuringTheBriefContactImmunity()
    {
        var combat = new TabletCombat();
        var launch = typeof(TabletCombat).GetMethod("LaunchHostile",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var origin = new Vector2(combat.Player.x, TabletCombat.HullTop + .006f);
        for (int i = 0; i < 3; i++) launch.Invoke(combat, new object[] { origin, Vector2.down, false });
        combat.Advance(.05);
        Assert.That(combat.Hp, Is.EqualTo(9));
        Assert.That(combat.HullContacts, Is.EqualTo(1));
        Assert.That(combat.InvulnerableContacts, Is.EqualTo(2));
        Assert.That(combat.ActiveHostiles, Is.Zero);
        combat.Advance(.36);
        launch.Invoke(combat, new object[] { origin, Vector2.down, false });
        combat.Advance(.05);
        Assert.That(combat.Hp, Is.EqualTo(8));
    }

    [Test]
    public void TheCarrierAndAllThreeVisibleMuzzlesStayFixedThroughCombatAndRetry()
    {
        var combat = new TabletCombat();
        Vector2[] before = { combat.CarrierMuzzle(0), combat.CarrierMuzzle(1), combat.CarrierMuzzle(2) };
        combat.ApplyMatch(1, 5);
        combat.Advance(12);
        Assert.That(combat.HullContacts, Is.Positive);
        Assert.That(combat.Player, Is.EqualTo(new Vector2(.5f, 0)));
        combat.Reset();
        for (int i = 0; i < before.Length; i++)
        {
            Assert.That(combat.CarrierMuzzle(i), Is.EqualTo(before[i]));
            Assert.That(before[i].y, Is.GreaterThan(0));
        }
    }

    [Test]
    public void WithoutInputTheCarrierFallsButDefenseCanSurviveWithoutExtraProjectiles()
    {
        var unprotected = new TabletCombat();
        unprotected.Advance(60);
        Assert.That(unprotected.Result, Is.EqualTo(TabletResult.Failed));
        Assert.That(unprotected.HullContacts, Is.EqualTo(10));
        Assert.That(unprotected.MissedHostiles, Is.Positive);
        Assert.That(unprotected.ActiveHostiles, Is.Zero);

        var defended = new TabletCombat();
        int peak = 0;
        while (defended.Result == TabletResult.Playing)
        {
            if (defended.Hp <= 7) defended.ApplyMatch(4, 3);
            if (defended.PaletteSize >= 6 && defended.Shield <= .25f) defended.ApplyMatch(6, 5);
            defended.Advance(.23);
            peak = System.Math.Max(peak, defended.ActiveHostiles);
        }
        Assert.That(defended.Result, Is.EqualTo(TabletResult.Survived));
        Assert.That(defended.OfferedShots, Is.Zero);
        Assert.That(defended.BlockedDamage, Is.GreaterThan(3));
        Assert.That(peak, Is.InRange(3, TabletCombat.HostileCapacity));
        Assert.That(defended.ActiveHostiles, Is.Zero);
    }

    [Test]
    public void CombatTypeExists()
    {
        Assert.That(typeof(Game).Assembly.GetType("TabletCombat"), Is.Not.Null);
    }

    [TestCase(1, 3, 0)]
    [TestCase(2, 5, 0)]
    [TestCase(3, 12, 0)]
    [TestCase(5, 5, 18)]
    [TestCase(7, 12, 42)]
    public void AttackQueuesExactlyOneProjectilePerTile(int color, int count, int unlock)
    {
        var combat = new TabletCombat();
        KeepAlive(combat, unlock);
        combat.ApplyMatch(color, count);
        Assert.That(combat.PendingShots, Is.EqualTo(count));
        Assert.That(combat.OfferedShots, Is.EqualTo(count));
    }

    [Test]
    public void InvalidAndLockedGroupsHaveNoCombatEffect()
    {
        var combat = new TabletCombat();
        combat.ApplyMatch(1, 2);
        combat.ApplyMatch(6, 12);
        combat.ApplyMatch(8, 12);
        Assert.That(combat.OfferedShots, Is.Zero);
        Assert.That(combat.Shield, Is.Zero);
    }

    [Test]
    public void DefenseCapsAndExpiresWithoutCreatingProjectiles()
    {
        var combat = new TabletCombat();
        combat.Advance(4.9);
        Assert.That(combat.Hp, Is.EqualTo(9));
        combat.ApplyMatch(4, 5);
        Assert.That(combat.Hp, Is.EqualTo(10));
        KeepAlive(combat, 30);
        combat.ApplyMatch(6, 3);
        Assert.That(combat.Shield, Is.EqualTo(2.25).Within(.001));
        combat.ApplyMatch(6, 5);
        combat.ApplyMatch(6, 8);
        Assert.That(combat.Shield, Is.EqualTo(6).Within(.001));
        int health = combat.Hp;
        combat.Advance(3.1);
        Assert.That(combat.Hp, Is.EqualTo(health));
        Assert.That(combat.BlockedDamage, Is.GreaterThan(0));
        combat.Advance(10);
        Assert.That(combat.Shield, Is.Zero);
        Assert.That(combat.Hp, Is.LessThan(health));
        Assert.That(combat.OfferedShots, Is.Zero);
    }

    [Test]
    public void CadencePreservesFifoHandoffAndPoolCapacity()
    {
        var combat = new TabletCombat();
        combat.Advance(1);
        combat.ApplyMatch(1, 3);
        combat.ApplyMatch(2, 12);
        combat.Advance(.19);
        Assert.That(combat.FiredShots, Is.EqualTo(3));
        Assert.That(combat.TargetIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(combat.LastFiredColor, Is.EqualTo(1));
        combat.Advance(.12);
        Assert.That(combat.LastFiredColor, Is.EqualTo(2));
        combat.Advance(.68);
        Assert.That(combat.FiredShots, Is.LessThanOrEqualTo(12));
        Assert.That(combat.PendingShots + combat.FiredShots, Is.EqualTo(15));
        Assert.That(combat.ActiveShots, Is.LessThanOrEqualTo(TabletCombat.ShotCapacity));
    }

    [Test]
    public void YellowSharesItsFanAndOrangeVioletDamageIsBounded()
    {
        Assert.That(TabletCombat.SpreadAngle(0, 3), Is.LessThan(0));
        Assert.That(TabletCombat.SpreadAngle(1, 3), Is.Zero);
        Assert.That(TabletCombat.SpreadAngle(2, 3), Is.EqualTo(-TabletCombat.SpreadAngle(0, 3)));
        Assert.That(TabletCombat.HitPower(1, 0), Is.EqualTo(1));
        Assert.That(TabletCombat.HitPower(2, 1), Is.EqualTo(.5f));
        Assert.That(TabletCombat.HitPower(3, 0), Is.EqualTo(1));
        Assert.That(TabletCombat.HitPower(5, 0), Is.EqualTo(.8f));
        Assert.That(TabletCombat.HitPower(7, 0), Is.EqualTo(1));
        Assert.That(TabletCombat.HitPower(7, 1), Is.EqualTo(.35f));
    }

    [Test]
    public void TimedPaletteAndWaveHealthFollowTheApprovedSchedule()
    {
        var combat = new TabletCombat();
        Assert.That(combat.PaletteSize, Is.EqualTo(3));
        KeepAlive(combat, 4);
        Assert.That(combat.PaletteSize, Is.EqualTo(4));
        KeepAlive(combat, 18);
        Assert.That(combat.PaletteSize, Is.EqualTo(5));
        Assert.That(combat.Wave, Is.EqualTo(2));
        KeepAlive(combat, 30);
        Assert.That(combat.PaletteSize, Is.EqualTo(6));
        KeepAlive(combat, 38);
        Assert.That(combat.Wave, Is.EqualTo(3));
        KeepAlive(combat, 42);
        Assert.That(combat.PaletteSize, Is.EqualTo(7));
        Assert.That(combat.TotalStartingEnemyHp, Is.EqualTo(44f));
    }

    [Test]
    public void EnemyEntersFromAboveAndBrakesIntoTheArena()
    {
        var combat = new TabletCombat();
        combat.Advance(1.0 / 60);
        Vector2 entry = combat.Enemies[0].Position;
        combat.Advance(.35);
        Vector2 moving = combat.Enemies[0].Position;
        combat.Advance(.48);
        Vector2 settled = combat.Enemies[0].Position;
        combat.Advance(.20);

        Assert.That(entry.y, Is.GreaterThan(1.02f));
        Assert.That(settled.y, Is.InRange(.81f, .85f));
        Assert.That(Vector2.Distance(entry, moving), Is.GreaterThan(5 * Vector2.Distance(settled, combat.Enemies[0].Position)));
    }

    [Test]
    public void WingmatesEnterInOrderAndTheLeadShipReturnsAfterDiving()
    {
        var combat = new TabletCombat();
        combat.Advance(.05);
        Assert.That(combat.Enemies[0].Position.x, Is.LessThan(0));
        Assert.That(combat.Enemies[1].Spawned, Is.False);
        combat.Advance(.32);
        Assert.That(combat.Enemies[1].Spawned, Is.True);
        Assert.That(combat.Enemies[0].Position.x, Is.InRange(0f, 1f));
        combat.Advance(.63);
        float formationY = combat.Enemies[0].Position.y;
        combat.Advance(.60);
        Assert.That(combat.Enemies[0].Position.y, Is.LessThan(formationY - .12f));
        combat.Advance(.80);
        Assert.That(combat.Enemies[0].Position.y, Is.EqualTo(formationY).Within(.08f));
        Assert.That(combat.Hp, Is.EqualTo(TabletCombat.MaxHp));
    }

    [Test]
    public void EveryFlightRemainsTargetableAfterEntering()
    {
        var combat = new TabletCombat();
        KeepAlive(combat, 50);
        foreach (var enemy in combat.Enemies)
        {
            Assert.That(enemy.Spawned, Is.True);
            Assert.That(enemy.Position.x, Is.InRange(.04f, .96f));
            Assert.That(enemy.Position.y, Is.InRange(.20f, .96f));
        }
        Assert.That(combat.TotalStartingEnemyHp, Is.EqualTo(44));
    }

    [Test]
    public void FlightAwardRequiresEveryWingmateAndSurvivesAFailedRoundOnlyUntilReset()
    {
        var combat = new TabletCombat();
        combat.ApplyMatch(1, 8);
        for (int i = 0; i < 240 && combat.EnemiesDefeated < 1; i++) combat.Advance(1.0 / 60);
        Assert.That(combat.EnemiesDefeated, Is.EqualTo(1));
        Assert.That(combat.AwardedFlights, Is.Zero);
        for (int i = 0; i < 240 && (combat.AwardedFlights & 1) == 0; i++) combat.Advance(1.0 / 60);
        Assert.That(combat.AwardedFlights, Is.EqualTo(1));
        combat.Advance(60);
        Assert.That(combat.Result, Is.EqualTo(TabletResult.Failed));
        Assert.That(combat.AwardedFlights & TabletProgress.BossBit, Is.Zero);
        Assert.That(combat.AwardedFlights & 1, Is.EqualTo(1));
        combat.Reset();
        Assert.That(combat.AwardedFlights, Is.Zero);
    }

    [Test]
    public void HullUpgradeRaisesTheHealingCeilingButDoesNotChangeHealingOrShieldPerTile()
    {
        var combat = new TabletCombat();
        combat.Reset(13, 1.3f);
        combat.Advance(4.9);
        Assert.That(combat.Hp, Is.EqualTo(12));
        combat.ApplyMatch(4, 3);
        Assert.That(combat.Hp, Is.EqualTo(13));
        KeepAlive(combat, 30);
        combat.ApplyMatch(6, 3);
        Assert.That(combat.Shield, Is.EqualTo(2.25).Within(.001));
        Assert.That(combat.OfferedShots, Is.Zero);
    }

    [Test]
    public void NonlethalContactShowsAVisibleImpactOnTheDamageTick()
    {
        var combat = new TabletCombat();
        combat.ApplyMatch(1, 3);
        for (int frame = 0; frame < 60 && combat.HitShots == 0; frame++)
            combat.Advance(1.0 / 60);

        Assert.That(combat.HitShots, Is.EqualTo(1));
        Assert.That(combat.TotalDamage, Is.EqualTo(1));
        Assert.That(combat.EnemiesDefeated, Is.Zero);
        Assert.That(combat.ActiveEffects, Is.EqualTo(1));
        combat.Reset();
        Assert.That(combat.ActiveEffects, Is.Zero);
    }

    [Test]
    public void OpeningShotReachesTheScoutWithinAQuarterOfASecond()
    {
        var combat = new TabletCombat();
        combat.Advance(.9);
        combat.ApplyMatch(1, 3);
        combat.Advance(.25);
        Assert.That(combat.FiredShots, Is.EqualTo(3));
        Assert.That(combat.HitShots, Is.GreaterThan(0));
    }

    [Test]
    public void RoundResultsPrioritizeLethalDamageAndResetAllPooledState()
    {
        var combat = new TabletCombat();
        combat.Advance(60);
        Assert.That(combat.Result, Is.EqualTo(TabletResult.Failed));
        Assert.That(TabletCombat.Resolve(0, 60, true), Is.EqualTo(TabletResult.Failed));
        combat.Reset();
        KeepAlive(combat, 60);
        Assert.That(combat.Result, Is.EqualTo(TabletResult.Survived));
        Assert.That(combat.PendingShots + combat.ActiveShots, Is.Zero);
        Assert.That(combat.Shield, Is.Zero);
        combat.Reset();
        combat.ApplyMatch(1, 45);
        combat.ApplyMatch(1, 45);
        for (int second = 0; second < 60 && combat.Result == TabletResult.Playing; second++)
        {
            if (second >= 4) combat.ApplyMatch(4, 4);
            if (second >= 30) combat.ApplyMatch(6, 6);
            combat.Advance(1);
        }
        Assert.That(combat.Result, Is.EqualTo(TabletResult.Cleared));
        Assert.That(combat.EnemiesDefeated, Is.EqualTo(24));
        Assert.That(combat.TotalDamage, Is.EqualTo(44).Within(.001));
        Assert.That(combat.AwardedFlights, Is.EqualTo(31));
        Assert.That(TabletProgress.RewardTotal(combat.AwardedFlights), Is.EqualTo(180));
        Assert.That(combat.ActiveShots + combat.PendingShots, Is.Zero);
        combat.Reset();
        Assert.That(combat.Elapsed, Is.Zero);
        Assert.That(combat.Hp, Is.EqualTo(10));
        Assert.That(combat.PaletteSize, Is.EqualTo(3));
        Assert.That(combat.FiredShots, Is.Zero);
        Assert.That(combat.ActiveEffects, Is.Zero);
        Assert.That(combat.ActiveHostiles + combat.FiredHostiles, Is.Zero);
    }

    static void KeepAlive(TabletCombat combat, int until)
    {
        while (combat.Elapsed + .001f < until)
        {
            if (combat.PaletteSize >= 4) combat.ApplyMatch(4, 4);
            combat.Advance(System.Math.Min(.5, until - combat.Elapsed));
        }
    }
}
