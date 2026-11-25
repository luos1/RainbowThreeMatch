using UnityEngine;

internal enum TabletBossPhase { None, Approaching, Warning, Counterattack, Recovery }

internal sealed partial class TabletCombat
{
    const float FormationBreather = 1.25f;
    const float BossApproachTime = 1.65f;
    const float BossChargeTime = 1.1f;
    const float BossCounterTime = 1.6f;
    const int WaveTwoStart = 10;
    const int WaveThreeStart = 16;
    internal const float BossCounterMultiplier = 1.5f;

    double bossFiredAt;
    bool BossCounterOpen => elapsed < bossFiredAt + BossCounterTime;

    internal int Wave => elapsed >= roundPlans[WaveThreeStart].Spawn ? 3
        : elapsed >= roundPlans[WaveTwoStart].Spawn ? 2 : 1;

    internal bool BossApproaching => BossPhase == TabletBossPhase.Approaching;

    internal TabletBossPhase BossPhase
    {
        get
        {
            if (Result != TabletResult.Playing) return TabletBossPhase.None;
            var plan = roundPlans[EnemyCount - 1];
            var boss = Boss;
            if (!boss.Spawned)
                return elapsed >= plan.Spawn - BossApproachTime ? TabletBossPhase.Approaching : TabletBossPhase.None;
            if (boss.Hp <= 0) return TabletBossPhase.None;
            if (BossCounterOpen) return TabletBossPhase.Counterattack;
            if (!boss.Attacked && elapsed >= FireAt(plan, boss) - BossChargeTime) return TabletBossPhase.Warning;
            return boss.Volleys == 0 ? TabletBossPhase.Approaching : TabletBossPhase.Recovery;
        }
    }

    void ResetPacing() => bossFiredAt = double.NegativeInfinity;

    static float WarningTime(EnemyPlan plan) => IsBoss(plan) ? BossChargeTime : HostileWarning;

    void RecordBossSalvo() => bossFiredAt = elapsed;

    float DamageMultiplier(int index) => index == EnemyCount - 1 && BossCounterOpen
        ? BossCounterMultiplier : 1f;

    void ShortenFormationGap()
    {
        for (int i = EnemyCount; i < planCount; i++)
            if (IsAlive(i)) return;

        int next = 0;
        while (next < EnemyCount && enemies[next].Spawned)
        {
            if (IsAlive(next)) return;
            next++;
        }
        if (next == EnemyCount || !roundPlans[next].StartsFormation) return;

        var lead = roundPlans[next];
        float gap = FormationBreather + (IsBoss(lead) ? BossApproachTime : 0);
        float advance = lead.Spawn - (Elapsed + gap);
        if (advance <= 0) return;

        int end = next + 1;
        while (end < EnemyCount && !roundPlans[end].StartsFormation) end++;

        // Keep the formation's entry spacing and attack preparation time together.
        float lastSpawn = roundPlans[end - 1].Spawn;
        for (int i = next; i < end; i++)
            roundPlans[i] = roundPlans[i].ShiftEarlier(advance);

        // Copies already queued for this formation share its adjusted entry time.
        for (int i = EnemyCount; i < planCount; i++)
        {
            float spawn = roundPlans[i].Spawn;
            if (!enemies[i].Spawned && spawn >= lead.Spawn && spawn <= lastSpawn + EntryTime)
                roundPlans[i] = roundPlans[i].ShiftEarlier(advance);
        }
    }
}
