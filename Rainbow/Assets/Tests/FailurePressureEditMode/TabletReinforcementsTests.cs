using System.Linq;
using NUnit.Framework;

public class TabletReinforcementsTests
{
    [Test]
    public void SuccessfulMatchClearsTheWarningAndPreservesShotCount()
    {
        var combat = new TabletCombat();
        combat.RegisterMiss();
        Assert.That(combat.ReinforcementWarning, Is.False);
        combat.RegisterMiss();
        Assert.That(combat.ReinforcementWarning, Is.True);
        Assert.That(combat.PlannedEnemies, Is.EqualTo(TabletCombat.EnemyCount));

        combat.ApplyMatch(1, 5);
        Assert.That(combat.FailureStreak, Is.Zero);
        Assert.That(combat.ReinforcementWarning, Is.False);
        Assert.That(combat.OfferedShots, Is.EqualTo(5));
        combat.RegisterMiss();
        Assert.That(combat.ReinforcementWarning, Is.False);
    }

    [Test]
    public void ThirdMissLaunchesBothSidesWithoutAdvancingTheClock()
    {
        var combat = new TabletCombat();
        combat.Reset(hull: 100);
        combat.Advance(1);
        int opening = combat.Enemies.Count(enemy => enemy.Spawned);
        Assert.That(opening, Is.EqualTo(5));

        combat.RegisterMiss();
        combat.RegisterMiss();
        Assert.That(combat.RegisterMiss(), Is.EqualTo(TabletMissOutcome.Deployed));
        Assert.That(combat.Elapsed, Is.EqualTo(1));
        Assert.That(combat.FailureStreak, Is.Zero);
        Assert.That(combat.ReinforcementWarning, Is.False);
        Assert.That(combat.ReinforcementDeploying, Is.True);
        Assert.That(combat.FiredHostiles, Is.Zero);

        var wave = combat.Enemies.Skip(opening).Take(5);
        var mirrored = combat.Enemies.Skip(TabletCombat.EnemyCount).Take(5);
        Assert.That(wave.All(enemy => enemy.Spawned && enemy.Hp > 0 && enemy.Position.x > .5f), Is.True);
        Assert.That(mirrored.All(enemy => enemy.Spawned && enemy.Hp > 0 && enemy.Position.x < .5f), Is.True);
        Assert.That(combat.Enemies.Count(enemy => enemy.Spawned), Is.EqualTo(opening + 10));

        combat.Advance(.65);
        Assert.That(combat.ReinforcementsArrived, Is.EqualTo(5));
        Assert.That(combat.Enemies.Count(enemy => enemy.Spawned && enemy.Position.y <= 1), Is.EqualTo(opening + 10));

        combat.Advance(7);
        Assert.That(combat.Enemies.Count(enemy => enemy.Spawned), Is.EqualTo(opening + 10));
        Assert.That(combat.ReinforcementDeploying, Is.False);
    }

    [Test]
    public void SecondDeploymentAdvancesTheNextFormationAndResetRestoresTheSchedule()
    {
        var combat = new TabletCombat();
        for (int formation = 0; formation < 2; formation++)
        {
            combat.RegisterMiss();
            combat.RegisterMiss();
            Assert.That(combat.RegisterMiss(), Is.EqualTo(TabletMissOutcome.Deployed));
            combat.Advance(.65);
        }

        Assert.That(combat.ReinforcementsArrived, Is.EqualTo(9));
        Assert.That(combat.Enemies.Count(enemy => enemy.Spawned), Is.EqualTo(23));
        combat.Reset();
        combat.Advance(1.3);
        Assert.That(combat.ReinforcementDeploying, Is.False);
        Assert.That(combat.ReinforcementsArrived, Is.Zero);
        Assert.That(combat.Enemies.Count(enemy => enemy.Spawned), Is.EqualTo(5));
    }
}
