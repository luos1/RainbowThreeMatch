using System;
using UnityEngine;

internal enum TabletMissOutcome { Streak, Queued, SupportQueued, AlreadyQueued, SupportUsed }

internal sealed partial class TabletCombat
{
    internal const int MissLimit = 3;
    const int SupportSize = 3;
    // The opening group already enters at launch. The last marker is the boss index.
    static readonly int[] ReinforcementBatches = { 5, 10, 14, 16, 21, 23 };
    static readonly float[] SupportLanes = { .23f, .50f, .77f };

    internal static int EnemyCapacity => EnemyCount + EnemyCount - 1 - ReinforcementBatches[0] + SupportSize;

    readonly EnemyPlan[] roundPlans = new EnemyPlan[EnemyCapacity];
    int planCount;
    bool supportUsed;

    internal int FailureStreak { get; private set; }
    internal int ReinforcementsPending { get; private set; }
    internal int ReinforcementsArrived { get; private set; }
    internal int PlannedEnemies => planCount;

    internal TabletMissOutcome RegisterMiss()
    {
        if (Result != TabletResult.Playing || ++FailureStreak < MissLimit)
            return TabletMissOutcome.Streak;

        FailureStreak = 0;
        if (ReinforcementsPending > 0)
            return TabletMissOutcome.AlreadyQueued;

        for (int batch = 0; batch + 1 < ReinforcementBatches.Length; batch++)
        {
            int first = ReinforcementBatches[batch];
            if (roundPlans[first].Spawn <= elapsed) continue;
            int last = ReinforcementBatches[batch + 1];
            for (int i = first; i < last; i++)
            {
                var plan = roundPlans[i];
                AddReinforcement(new EnemyPlan(plan.Spawn + .11f, plan.Attack + .36f,
                    1 - plan.Lane, plan.Hp, plan.Style, plan.Flight, plan.Dives, true));
            }
            return TabletMissOutcome.Queued;
        }

        if (supportUsed)
            return TabletMissOutcome.SupportUsed;

        supportUsed = true;
        // Bring the reserve already into view at the end of the round.
        // Its first shot still has a warning window and has to travel to the carrier.
        for (int i = 0; i < SupportSize; i++)
            AddReinforcement(new EnemyPlan(Elapsed - EntryTime, Elapsed + .85f + i * .26f,
                SupportLanes[i], 1, 0, 3, false, true));
        return TabletMissOutcome.SupportQueued;
    }

    void AddReinforcement(EnemyPlan plan)
    {
        roundPlans[planCount++] = plan;
        ReinforcementsPending++;
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
        supportUsed = false;
    }
}
