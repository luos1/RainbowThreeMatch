using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed partial class TabletCombat
{
    internal const int HostileCapacity = 32;
    internal const float HullTop = .128f, HullBaseHalfWidth = .256f, HullTopHalfWidth = .079f;
    internal const float HullShoulder = .084f, HullShoulderHalfWidth = .214f;
    internal const float ShieldTop = .172f, ShieldBaseHalfWidth = .305f, ShieldTopHalfWidth = .131f;
    internal const float ShieldShoulder = .105f, ShieldShoulderHalfWidth = .257f;
    internal const float ContactFlashTime = .16f;
    internal const float CarrierSpriteWidth = .62f, CarrierSpriteHeight = .58f, CarrierSpriteCenter = -.075f;
    const float HostileWarning = .75f;
    const float BossSalvoInterval = 3.4f;
    const float EnemyMuzzleFlashTime = .13f;

    readonly HostileShot[] hostiles = new HostileShot[HostileCapacity];
    readonly float[] muzzleTimes = new float[3];

    internal IReadOnlyList<HostileShot> Hostiles => hostiles;
    internal IReadOnlyList<float> MuzzleTimes => muzzleTimes;
    internal int ActiveHostiles { get; private set; }
    internal int FiredHostiles { get; private set; }
    internal int MissedHostiles { get; private set; }
    internal int HullContacts { get; private set; }
    internal int InvulnerableContacts { get; private set; }
    internal float HullFlash { get; private set; }
    internal float ShieldFlash { get; private set; }
    internal Vector2 ContactPosition { get; private set; }

    double FireAt(EnemyPlan plan, Enemy enemy) => IsBoss(plan) && enemy.Volleys > 0
        ? bossFiredAt + BossSalvoInterval : plan.Attack;

    static bool IsBoss(EnemyPlan plan) => plan.Flight == BossFlight;

    internal static float Bank(Enemy enemy) => Mathf.Clamp(-enemy.Velocity.x * 58f, -24f, 24f);

    internal static Vector2 EnemyMuzzle(Enemy enemy) =>
        enemy.Position + Rotate(Vector2.down, Bank(enemy)) * (enemy.Style == 2 ? .082f : .029f);

    internal Vector2 CarrierMuzzle(int index) => Player + (index == 0
        ? new Vector2(0, .210f)
        : new Vector2(index == 1 ? -.162f : .162f, .083f));

    void ResetHostileStats()
    {
        FiredHostiles = MissedHostiles = HullContacts = InvulnerableContacts = 0;
        ContactPosition = Vector2.zero;
    }

    void ClearHostiles()
    {
        Array.Clear(hostiles, 0, hostiles.Length);
        Array.Clear(muzzleTimes, 0, muzzleTimes.Length);
        ActiveHostiles = 0;
        HullFlash = ShieldFlash = 0;
    }

    void UpdateHostileFire(float delta)
    {
        HullFlash = Mathf.Max(0, HullFlash - delta);
        ShieldFlash = Mathf.Max(0, ShieldFlash - delta);
        for (int i = 0; i < muzzleTimes.Length; i++)
            muzzleTimes[i] = Mathf.Max(0, muzzleTimes[i] - delta);

        for (int i = 0; i < hostiles.Length; i++)
        {
            ref var shot = ref hostiles[i];
            if (!shot.Active) continue;
            Vector2 step = shot.Direction * (shot.Speed * delta);
            bool shield = Shield > 0;
            if (IntersectsBow(shot.Position - Player, shot.Position + step - Player, shield, out float enter))
            {
                ContactPosition = shot.Position + step * enter;
                if (shield)
                {
                    BlockedDamage++;
                    ShieldFlash = ContactFlashTime;
                }
                else if (elapsed >= invulnerableUntil)
                {
                    HullContacts++;
                    Hp = Math.Max(0, Hp - 1);
                    HullFlash = ContactFlashTime;
                    invulnerableUntil = elapsed + .35;
                }
                else
                    InvulnerableContacts++;

                shot.Active = false;
                ActiveHostiles--;
                continue;
            }
            shot.Position += step;
            if (shot.Position.y < 0 || shot.Position.x < -.04f || shot.Position.x > 1.04f)
            {
                shot.Active = false;
                ActiveHostiles--;
                MissedHostiles++;
            }
        }

        for (int i = 0; i < planCount; i++)
        {
            var plan = roundPlans[i];
            ref var enemy = ref enemies[i];
            if (!IsTargetable(i) || enemy.Attacked || elapsed < FireAt(plan, enemy)) continue;
            bool boss = IsBoss(plan);
            int count = boss ? 3 : 1;
            if (ActiveHostiles + count > HostileCapacity) continue;
            Vector2 origin = EnemyMuzzle(enemy);
            if (origin.x < 0 || origin.x > 1 || origin.y <= 0 || origin.y > 1) continue;
            // Only the direction chosen at launch matters. Offset fire can pass the bow.
            Vector2 destination = Player + new Vector2(0, .034f);
            Vector2 direction = (destination - origin).normalized;
            if (boss)
            {
                float center = enemy.Volleys % 2 == 0 ? -9 : 9;
                for (int shell = -1; shell <= 1; shell++)
                    LaunchHostile(origin, Rotate(direction, center + shell * 18), true);
                RecordBossSalvo();
            }
            else
            {
                // A few wingmates rake across the shoulders while their lead aims at the center.
                float offset = i % 5 == 4 ? (i % 2 == 0 ? -24 : 24) : i % 3 == 2 ? 7 : 0;
                LaunchHostile(origin, Rotate(direction, offset), false);
            }
            enemy.Attacked = ++enemy.Volleys >= (boss ? 3 : 1);
            enemy.Warning = false;
            enemy.MuzzleTime = EnemyMuzzleFlashTime;
        }
    }

    void LaunchHostile(Vector2 origin, Vector2 direction, bool boss)
    {
        int index = Array.FindIndex(hostiles, shot => !shot.Active);
        hostiles[index] = new HostileShot
        {
            Active = true, Boss = boss, Position = origin, Direction = direction,
            Speed = boss ? .62f : .55f
        };
        ActiveHostiles++;
        FiredHostiles++;
    }

    internal static bool IntersectsBow(Vector2 from, Vector2 to, bool shield, out float enter)
    {
        float ceiling = shield ? ShieldTop : HullTop;
        float bottom = shield ? ShieldBaseHalfWidth : HullBaseHalfWidth;
        float top = shield ? ShieldTopHalfWidth : HullTopHalfWidth;
        float shoulder = shield ? ShieldShoulder : HullShoulder;
        float middle = shield ? ShieldShoulderHalfWidth : HullShoulderHalfWidth;
        float upperSlope = (middle - top) / (ceiling - shoulder);
        enter = 0;
        float exit = 1;

        // Clip against the visible bow silhouette in the carrier's frame of reference.
        return Clip(-from.y, -to.y, ref enter, ref exit)
            && Clip(from.y - ceiling, to.y - ceiling, ref enter, ref exit)
            && ClipWalls(from, to, (bottom - middle) / shoulder, bottom, ref enter, ref exit)
            && ClipWalls(from, to, upperSlope, middle + shoulder * upperSlope, ref enter, ref exit);
    }

    static bool ClipWalls(Vector2 from, Vector2 to, float slope, float width, ref float enter, ref float exit)
    {
        return Clip(from.x + from.y * slope - width, to.x + to.y * slope - width, ref enter, ref exit)
            && Clip(-from.x + from.y * slope - width, -to.x + to.y * slope - width, ref enter, ref exit);
    }

    static bool Clip(float start, float end, ref float enter, ref float exit)
    {
        if (start <= 0 && end <= 0) return true;
        if (start > 0 && end > 0) return false;
        float crossing = start / (start - end);
        if (start > 0) enter = Mathf.Max(enter, crossing);
        else exit = Mathf.Min(exit, crossing);
        return enter <= exit;
    }

    internal struct HostileShot
    {
        internal bool Active, Boss;
        internal float Speed;
        internal Vector2 Position, Direction;
    }
}
