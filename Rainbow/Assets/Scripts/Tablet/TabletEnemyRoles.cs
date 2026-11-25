using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("Rainbow.EnemyRoles.EditModeTests")]

// These values follow the existing scout, wing and elite sprite indices.
internal enum TabletEnemyRole { Scout, Gunner, Boss }

internal sealed partial class TabletCombat
{
    const float GunnerWarning = 2.2f;
    internal const int GunnerDamage = 2;

    static float WarningLead(TabletEnemyRole role) =>
        role == TabletEnemyRole.Gunner ? GunnerWarning : HostileWarning;

    internal (int Index, float Seconds) NextEnemyAttack()
    {
        if (Result != TabletResult.Playing) return (-1, 0);
        int next = -1;
        float nextFire = float.PositiveInfinity;
        bool prioritizingGunner = false;
        for (int i = 0; i < planCount; i++)
        {
            var enemy = enemies[i];
            if (!enemy.Warning || enemy.Role == TabletEnemyRole.Boss || !IsTargetable(i)) continue;
            bool gunner = enemy.Role == TabletEnemyRole.Gunner;
            if (prioritizingGunner && !gunner) continue;
            float fire = FireAt(roundPlans[i], enemy);
            if (gunner == prioritizingGunner && fire >= nextFire) continue;
            next = i;
            nextFire = fire;
            prioritizingGunner = gunner;
        }
        return next < 0 ? (-1, 0) : (next, Mathf.Max(0, nextFire - Elapsed));
    }

    internal float? NextHeavyShellApproach()
    {
        if (Result != TabletResult.Playing) return null;
        const float projection = 3f;
        float soonest = float.PositiveInfinity;
        foreach (var shell in hostiles)
        {
            if (!shell.Active || !shell.Heavy) continue;
            var from = shell.Position - Player;
            var to = from + shell.Direction * (shell.Speed * projection);
            // Show the time to the shield boundary, where a blue match could intercept the shell.
            if (IntersectsBow(from, to, true, out float enter))
                soonest = Mathf.Min(soonest, enter * projection);
        }
        return float.IsPositiveInfinity(soonest) ? (float?)null : soonest;
    }
}
