using System;
using System.Collections.Generic;
using UnityEngine;

internal enum TabletResult { Playing, Failed, Survived, Cleared }

internal sealed class TabletCombat
{
    internal const int MaxHp = 10;
    internal const int Duration = 60;
    internal const int ShotCapacity = 48;
    internal const int EffectCapacity = 10;
    internal const float MaxShield = 6;
    internal const float ImpactTime = .09f;
    const float EntryTime = .66f;
    const float DiveTime = .92f;
    const float DivePeriod = 5.35f;
    const float AimLead = .50f;
    const float AimRefresh = .14f;
    const float PlayerSteerSpeed = .88f;
    const float BurstTime = .22f;
    const float ShotSpeed = 3.65f;
    const float HomingTurnRate = 240f;
    const float HitRadius = .065f;
    const double Frame = 1.0 / 60;
    const double ShotInterval = 1.0 / 12;

    // Flight identifiers also select alternating entry sides. Strike times stay independent of motion.
    static readonly EnemyPlan[] Plans =
    {
        new EnemyPlan(0, 9, .41f, 2, 1, 0, 0, true), new EnemyPlan(.24f, 13, .58f, 2, 1, 0, 0),
        new EnemyPlan(6.1f, 17, .27f, 2, 1, 0, 1, true), new EnemyPlan(6.34f, 21, .74f, 2, 1, 0, 1),
        new EnemyPlan(18, 23, .31f, 3, 1, 1, 2, true), new EnemyPlan(18.24f, 28, .66f, 3, 1, 1, 2),
        new EnemyPlan(18.48f, 33, .51f, 3, 2, 1, 2, true),
        new EnemyPlan(38, 43, .26f, 3, 2, 0, 3, true), new EnemyPlan(38.24f, 47, .74f, 3, 2, 0, 3),
        new EnemyPlan(38.48f, 51, .56f, 3, 2, 0, 3, true), new EnemyPlan(46.25f, 57, .49f, 6, 2, 2, 4, true)
    };

    readonly Queue<Volley> queue = new Queue<Volley>();
    readonly Shot[] shots = new Shot[ShotCapacity];
    readonly Enemy[] enemies = new Enemy[Plans.Length];
    readonly Effect[] effects = new Effect[EffectCapacity];
    double elapsed, nextShot, aimUntil, invulnerableUntil;
    int target = -1;
    int nextEffect;

    internal float Elapsed => (float)elapsed;
    internal float Remaining => Duration - Elapsed;
    internal int Hp { get; private set; }
    internal float Shield { get; private set; }
    internal int PaletteSize => Elapsed >= 42 ? 7 : Elapsed >= 30 ? 6 : Elapsed >= 18 ? 5 : Elapsed >= 4 ? 4 : 3;
    internal int Wave => Elapsed >= 38 ? 3 : Elapsed >= 18 ? 2 : 1;
    internal TabletResult Result { get; private set; }
    internal Vector2 Player { get; private set; }
    internal IReadOnlyList<Shot> Shots => shots;
    internal IReadOnlyList<Enemy> Enemies => enemies;
    internal IReadOnlyList<Effect> Effects => effects;
    internal int TargetIndex => target;
    internal int PendingShots { get; private set; }
    internal int ActiveShots { get; private set; }
    internal int ActiveEffects
    {
        get
        {
            int count = 0;
            foreach (var effect in effects)
                if (effect.Lifetime > 0) count++;
            return count;
        }
    }
    internal int FiredShots { get; private set; }
    internal int OfferedShots { get; private set; }
    internal int HitShots { get; private set; }
    internal int ExpiredShots { get; private set; }
    internal int DroppedOnEnd { get; private set; }
    internal int BlockedDamage { get; private set; }
    internal int EnemiesDefeated { get; private set; }
    internal int LastFiredColor { get; private set; }
    internal float TotalDamage { get; private set; }
    internal float TotalStartingEnemyHp
    {
        get
        {
            float sum = 0;
            foreach (var plan in Plans)
                sum += plan.Hp;
            return sum;
        }
    }

    internal TabletCombat() => Reset();

    internal void Reset()
    {
        elapsed = nextShot = aimUntil = invulnerableUntil = 0;
        nextEffect = 0;
        Hp = MaxHp;
        EnemiesDefeated = OfferedShots = FiredShots = HitShots = ExpiredShots = DroppedOnEnd = BlockedDamage = LastFiredColor = 0;
        TotalDamage = 0;
        Player = new Vector2(.5f, .12f);
        Result = TabletResult.Playing;
        Array.Clear(enemies, 0, enemies.Length);
        ClearTransient();
    }

    void ClearTransient()
    {
        queue.Clear();
        Array.Clear(shots, 0, shots.Length);
        Array.Clear(effects, 0, effects.Length);
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].Velocity = Vector2.zero;
            enemies[i].HitTime = 0;
        }
        PendingShots = ActiveShots = 0;
        Shield = 0;
        target = -1;
    }

    internal void ApplyMatch(int color, int tileCount)
    {
        if (Result != TabletResult.Playing || color < 1 || color > PaletteSize || tileCount < MatchRules.MinimumGroupSize)
            return;
        switch (color)
        {
            case 4:
                Hp = Math.Min(MaxHp, Hp + tileCount);
                break;
            case 6:
                Shield = Math.Min(MaxShield, Shield + .75f * tileCount);
                break;
            default:
                queue.Enqueue(new Volley(color, tileCount));
                PendingShots += tileCount;
                OfferedShots += tileCount;
                break;
        }
    }

    internal void Advance(double seconds)
    {
        double until = Math.Min(Duration, elapsed + seconds);
        while (elapsed < until && Result == TabletResult.Playing)
        {
            double end = until - elapsed <= Frame + 1e-8 ? until : elapsed + Frame;
            float delta = (float)(end - elapsed);
            elapsed = end;
            Step(delta);
        }
    }

    void Step(float delta)
    {
        Shield = Mathf.Max(0, Shield - delta);
        for (int i = 0; i < effects.Length; i++)
            effects[i].Lifetime -= delta;

        for (int i = 0; i < Plans.Length; i++)
        {
            var plan = Plans[i];
            if (elapsed < plan.Spawn || (enemies[i].Spawned && enemies[i].Hp <= 0))
                continue;
            ref var enemy = ref enemies[i];
            Vector2 position = PositionAt(plan, i, Elapsed - plan.Spawn);
            enemy.Velocity = enemy.Spawned ? (position - enemy.Position) / Mathf.Max(delta, .0001f) : Vector2.zero;
            enemy.Position = position;
            enemy.HitTime = Mathf.Max(0, enemy.HitTime - delta);
            if (!enemy.Spawned)
            {
                enemy.Spawned = true;
                enemy.Style = plan.Style;
                enemy.Hp = enemy.MaxHp = plan.Hp;
            }

            enemy.Warning = !enemy.Attacked && elapsed >= plan.Attack - 1;
        }

        if (!IsTargetable(target) || elapsed >= aimUntil)
        {
            target = SelectTarget();
            aimUntil = elapsed + AimRefresh;
        }
        float destination = IsTargetable(target) ? enemies[target].Position.x : .5f;
        Player = new Vector2(Mathf.MoveTowards(Player.x, Mathf.Clamp(destination, .14f, .86f), PlayerSteerSpeed * delta), Player.y);

        UpdateShots(delta);
        if (PendingShots > 0 && IsTargetable(target) && elapsed + 1e-7 >= nextShot && ActiveShots < ShotCapacity)
            Fire();

        for (int i = 0; i < Plans.Length; i++)
        {
            if (!IsAlive(i) || enemies[i].Attacked || elapsed < Plans[i].Attack)
                continue;
            enemies[i].Attacked = true;
            enemies[i].Warning = false;
            TakeDamage(Plans[i].Damage);
        }

        Result = Resolve(Hp, elapsed, EnemiesDefeated == Plans.Length);
        if (Result != TabletResult.Playing)
        {
            DroppedOnEnd = PendingShots + ActiveShots;
            ClearTransient();
        }
    }

    internal static TabletResult Resolve(int hp, double seconds, bool cleared)
    {
        if (hp <= 0) return TabletResult.Failed;
        if (cleared) return TabletResult.Cleared;
        return seconds >= Duration ? TabletResult.Survived : TabletResult.Playing;
    }

    void TakeDamage(int damage)
    {
        if (Shield > 0)
        {
            BlockedDamage += damage;
            return;
        }
        if (elapsed >= invulnerableUntil)
        {
            Hp = Math.Max(0, Hp - damage);
            invulnerableUntil = elapsed + .35;
        }
    }

    int SelectTarget()
    {
        int best = -1;
        float distance = float.MaxValue;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (!IsTargetable(i)) continue;
            var offset = enemies[i].Position - Player;
            float score = offset.sqrMagnitude + (enemies[i].Attacked ? .18f : 0);
            if (score >= distance) continue;
            best = i;
            distance = score;
        }
        return best;
    }

    bool IsAlive(int index) => index >= 0 && enemies[index].Spawned && enemies[index].Hp > 0;

    bool IsTargetable(int index)
    {
        if (!IsAlive(index)) return false;
        var position = enemies[index].Position;
        return position.x >= 0 && position.x <= 1 && position.y >= 0 && position.y <= 1;
    }

    static Vector2 PositionAt(EnemyPlan plan, int index, float age)
    {
        bool left = plan.Flight % 2 == 0;
        Vector2 rally = new Vector2(plan.Lane + .022f * Mathf.Sin(age * 2.5f + index * .35f),
            .82f - .065f * (index % 2) + .016f * Mathf.Sin(age * 1.8f));
        if (age < EntryTime)
        {
            float phase = Mathf.Clamp01(age / EntryTime);
            // An eased cubic lets each flight cross the arena and brake into its slot.
            float t = 1 - Mathf.Pow(1 - phase, 1.65f);
            Vector2 start = left ? new Vector2(-.19f, 1.10f) : new Vector2(1.19f, 1.04f);
            Vector2 arch = left ? new Vector2(.02f, 1.21f) : new Vector2(.97f, 1.16f);
            Vector2 cross = left ? new Vector2(.91f, .50f) : new Vector2(.06f, .62f);
            float remaining = 1 - t;
            return remaining * remaining * (remaining * start + 3 * t * arch)
                + t * t * (3 * remaining * cross + t * rally);
        }

        float sinceDive = age - (EntryTime + .46f + index % 3 * .14f);
        if (!plan.Dives || sinceDive < 0) return rally;
        float dive = Mathf.Repeat(sinceDive, DivePeriod) / DiveTime;
        if (dive >= 1) return rally;

        float arc = Mathf.Sin(dive * Mathf.PI);
        float depth = plan.Style == 1 ? .29f : .255f;
        float turn = (left ? .19f : -.19f) * Mathf.Sin(dive * Mathf.PI * 2) * arc;
        return rally + new Vector2(turn, -depth * arc * arc);
    }

    void Fire()
    {
        var volley = queue.Peek();
        int index = Array.FindIndex(shots, shot => !shot.Active);
        Vector2 origin = Player + Vector2.up * .04f;
        var plan = Plans[target];
        float flight = Vector2.Distance(enemies[target].Position, origin) / ShotSpeed;
        var direction = (PositionAt(plan, target, Elapsed - plan.Spawn + flight * AimLead) - origin).normalized;
        if (volley.Color == 3)
            direction = Rotate(direction, SpreadAngle(volley.Fired, volley.Count));
        shots[index] = new Shot
        {
            Active = true, Color = volley.Color, Target = target, Position = origin,
            Direction = direction, Lifetime = 1.25f
        };
        nextShot = elapsed + ShotInterval;
        PendingShots--;
        ActiveShots++;
        FiredShots++;
        LastFiredColor = volley.Color;
        if (++volley.Fired == volley.Count)
            queue.Dequeue();
    }

    internal static float SpreadAngle(int index, int count) => count > 1 ? Mathf.Lerp(-11, 11, index / (count - 1f)) : 0;

    internal static float HitPower(int color, int contact) =>
        color == 5 ? .8f : color == 2 && contact > 0 ? .5f : color == 7 && contact > 0 ? .35f : 1f;

    void UpdateShots(float delta)
    {
        for (int index = 0; index < shots.Length; index++)
        {
            ref var shot = ref shots[index];
            if (!shot.Active) continue;
            if (shot.Color == 5)
            {
                if (!IsTargetable(shot.Target)) shot.Target = SelectTarget();
                if (IsTargetable(shot.Target))
                {
                    Vector2 desired = enemies[shot.Target].Position - shot.Position;
                    float angle = Vector2.SignedAngle(shot.Direction, desired);
                    shot.Direction = Rotate(shot.Direction, Mathf.Clamp(angle, -HomingTurnRate * delta, HomingTurnRate * delta));
                }
            }
            Vector2 step = shot.Direction * (ShotSpeed * delta);
            int hit = FindHit(shot, step);
            if (hit >= 0)
            {
                if (shot.Contacts == 0) HitShots++;
                shot.HitMask |= 1 << hit;
                HurtEnemy(hit, HitPower(shot.Color, shot.Contacts), shot.Color, shot.Direction);
                shot.Contacts++;
                if (shot.Color == 7)
                    Blast(hit);
                if (shot.Color != 2 || shot.Contacts >= 2)
                {
                    Release(ref shot);
                    continue;
                }
            }

            shot.Position += step;
            shot.Lifetime -= delta;
            if (shot.Lifetime <= 0 || shot.Position.y > 1.06f || shot.Position.x < -.07f || shot.Position.x > 1.07f)
                Release(ref shot);
        }
    }

    int FindHit(Shot shot, Vector2 step)
    {
        float squaredLength = step.sqrMagnitude;
        if (squaredLength <= 1e-12f) return -1;
        int best = -1;
        float nearest = float.MaxValue;
        for (int i = 0; i < enemies.Length; i++)
        {
            if (!IsTargetable(i) || (shot.HitMask & (1 << i)) != 0) continue;
            Vector2 relative = enemies[i].Position - shot.Position;
            float amount = Mathf.Clamp01(Vector2.Dot(relative, step) / squaredLength);
            float distance = (relative - amount * step).sqrMagnitude;
            if (distance > HitRadius * HitRadius || amount >= nearest) continue;
            best = i;
            nearest = amount;
        }
        return best;
    }

    void Blast(int impact)
    {
        Vector2 position = enemies[impact].Position;
        Emit(position, .19f, 7, true);
        int previous = -1;
        for (int count = 0; count < 2; count++)
        {
            float closest = .17f * .17f;
            int next = -1;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (i == impact || i == previous || !IsTargetable(i)) continue;
                float distance = (enemies[i].Position - position).sqrMagnitude;
                if (distance >= closest) continue;
                closest = distance;
                next = i;
            }
            if (next < 0) break;
            HurtEnemy(next, HitPower(7, 1), 7, (enemies[next].Position - position).normalized);
            previous = next;
        }
    }

    void HurtEnemy(int index, float damage, int color, Vector2 direction)
    {
        ref var enemy = ref enemies[index];
        float dealt = Mathf.Min(enemy.Hp, damage);
        TotalDamage += dealt;
        enemy.Hp -= dealt;
        enemy.HitTime = ImpactTime;
        enemy.HitColor = color;
        enemy.HitDirection = direction;
        bool destroyed = enemy.Hp <= 0;
        if (destroyed) EnemiesDefeated++;
        Emit(enemy.Position, destroyed ? .13f : .076f, color, destroyed);
    }

    void Emit(Vector2 position, float size, int color, bool burst)
    {
        float duration = burst ? BurstTime : ImpactTime;
        effects[nextEffect] = new Effect
        {
            Position = position, Size = size, Lifetime = duration, Duration = duration, Color = color, Burst = burst
        };
        nextEffect = (nextEffect + 1) % effects.Length;
    }

    void Release(ref Shot shot)
    {
        if (shot.Contacts == 0) ExpiredShots++;
        shot.Active = false;
        ActiveShots--;
    }

    static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float angle = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
    }

    internal struct Enemy
    {
        internal bool Spawned, Warning, Attacked;
        internal int Style, HitColor;
        internal float Hp, MaxHp, HitTime;
        internal Vector2 Position, Velocity, HitDirection;
    }

    internal struct Shot
    {
        internal bool Active;
        internal int Color, Target, Contacts, HitMask;
        internal float Lifetime;
        internal Vector2 Position, Direction;
    }

    internal struct Effect
    {
        internal float Lifetime, Duration, Size;
        internal int Color;
        internal bool Burst;
        internal Vector2 Position;
    }

    sealed class Volley
    {
        internal readonly int Color, Count;
        internal int Fired;
        internal Volley(int color, int count) { Color = color; Count = count; }
    }

    readonly struct EnemyPlan
    {
        internal readonly float Spawn, Attack, Lane, Hp;
        internal readonly int Damage, Style, Flight;
        internal readonly bool Dives;
        internal EnemyPlan(float spawn, float attack, float lane, float hp, int damage, int style, int flight, bool dives = false)
        {
            Spawn = spawn;
            Attack = attack;
            Lane = lane;
            Hp = hp;
            Damage = damage;
            Style = style;
            Flight = flight;
            Dives = dives;
        }
    }
}
