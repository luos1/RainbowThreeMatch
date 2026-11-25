using System;

internal enum TabletMissOutcome { Streak, Deployed, ReserveDeployed, AlreadyEntering, ReserveUsed }

internal sealed partial class TabletCombat
{
    internal const int MissLimit = 3;
    const float EntryHeadStart = .18f;
    const float ReserveEntryHeadStart = .24f;
    const float DeploymentNoticeTime = 1.2f;
    // The opening group already enters at launch. The last marker is the boss index.
    static readonly int[] ReinforcementBatches = { 5, 10, 14, 16, 21, 23 };
    static readonly float[] SupportLanes = { .23f, .50f, .77f };

    internal static int EnemyCapacity => EnemyCount + EnemyCount - 1 - ReinforcementBatches[0] + 2 * SupportLanes.Length;

    readonly EnemyPlan[] roundPlans = new EnemyPlan[EnemyCapacity];
    int planCount;
    bool reserveUsed;
    float deploymentUntil;

    internal int FailureStreak { get; private set; }
    internal int ReinforcementsPending { get; private set; }
    internal int ReinforcementsArrived { get; private set; }
    internal int PlannedEnemies => planCount;
    internal bool ReinforcementWarning => Result == TabletResult.Playing && FailureStreak == MissLimit - 1
        && ReinforcementsPending == 0 && !reserveUsed;
    internal bool ReinforcementDeploying => Result == TabletResult.Playing && Elapsed < deploymentUntil;

    internal TabletMissOutcome RegisterMiss()
    {
        if (Result != TabletResult.Playing || ++FailureStreak < MissLimit)
            return TabletMissOutcome.Streak;

        FailureStreak = 0;
        if (ReinforcementsPending > 0)
            return TabletMissOutcome.AlreadyEntering;

        int batch = NextFormation();
        if (batch >= 0)
        {
            int first = ReinforcementBatches[batch];
            int last = ReinforcementBatches[batch + 1];
            float now = Elapsed;
            float scheduledStart = Plans[first].Spawn;
            for (int i = first; i < last; i++)
            {
                var plan = Plans[i];
                // Start both sides inside the entry arc while preserving the original firing cadence.
                float spawn = now - EntryHeadStart + (plan.Spawn - scheduledStart) * .2f;
                float attack = now + (plan.Attack - scheduledStart);
                DeployEnemy(i, new EnemyPlan(spawn, attack, plan.Lane, plan.Hp, plan.Style, plan.Flight, plan.Dives));
                AddReinforcement(new EnemyPlan(spawn, attack + .36f,
                    1 - plan.Lane, plan.Hp, plan.Style, plan.Flight, plan.Dives, true));
            }
            deploymentUntil = now + DeploymentNoticeTime;
            return TabletMissOutcome.Deployed;
        }

        if (reserveUsed)
            return TabletMissOutcome.ReserveUsed;

        reserveUsed = true;
        // Start the reserve inside the visible part of the entry arc even in the final second.
        float reserveSpawn = Elapsed - ReserveEntryHeadStart;
        for (int i = 0; i < SupportLanes.Length; i++)
            AddReinforcement(new EnemyPlan(reserveSpawn, Elapsed + .85f + i * .26f,
                SupportLanes[i], 1, 0, 3, false, true));
        for (int i = 0; i < SupportLanes.Length; i++)
            AddReinforcement(new EnemyPlan(reserveSpawn, Elapsed + 1.21f + i * .26f,
                1 - SupportLanes[i], 1, 0, 2, false, true));
        deploymentUntil = Elapsed + DeploymentNoticeTime;
        return TabletMissOutcome.ReserveDeployed;
    }

    int NextFormation()
    {
        for (int batch = 0; batch + 1 < ReinforcementBatches.Length; batch++)
        {
            int first = ReinforcementBatches[batch];
            if (!enemies[first].Spawned && roundPlans[first].Spawn > elapsed)
                return batch;
        }
        return -1;
    }

    void AddReinforcement(EnemyPlan plan)
    {
        ReinforcementsPending++;
        DeployEnemy(planCount++, plan);
    }

    void DeployEnemy(int index, EnemyPlan plan)
    {
        roundPlans[index] = plan;
        ref var enemy = ref enemies[index];
        enemy.Position = PositionAt(plan, index, Elapsed - plan.Spawn);
        SpawnEnemy(ref enemy, plan);
        CountVisibleReinforcement(index, ref enemy);
    }

    void CountVisibleReinforcement(int index, ref Enemy enemy)
    {
        if (enemy.Reinforcement && !enemy.ReinforcementVisible && IsTargetable(index))
        {
            enemy.ReinforcementVisible = true;
            ReinforcementsPending--;
            ReinforcementsArrived++;
        }
    }

    void ResetReinforcements()
    {
        Array.Copy(Plans, roundPlans, Plans.Length);
        planCount = Plans.Length;
        ReinforcementsArrived = 0;
        reserveUsed = false;
        deploymentUntil = 0;
    }
}
