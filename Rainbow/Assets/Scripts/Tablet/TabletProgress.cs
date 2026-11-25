using System;
using UnityEngine;

internal enum TabletUpgrade { Attack, Hull }

internal sealed class TabletProgress
{
    internal const string DefaultKey = "Rainbow.TabletProgress.v1";
    internal const int BossReward = 75;
    internal const int BossBit = 1 << TabletCombat.BossFlight;
    const int AwardMask = (BossBit << 1) - 1;
    const int Version = 1;

    static readonly int[] Rewards = { 20, 20, 30, 35, BossReward };
    static readonly int[] AttackPrices = { 40, 75, 115 };
    static readonly int[] HullPrices = { 35, 70, 110 };

    readonly string key;
    Save data;

    internal TabletProgress(string key = DefaultKey)
    {
        this.key = key;
        data = Load();
    }

    internal int Balance => data.balance;
    internal int AttackLevel => data.attack;
    internal int HullLevel => data.hull;
    internal float Power => 1 + .1f * AttackLevel;
    internal int MaxHp => TabletCombat.MaxHp + HullLevel;

    internal static int RewardTotal(int mask)
    {
        int total = 0;
        for (int i = 0; i < Rewards.Length; i++)
            if ((mask & (1 << i)) != 0) total += Rewards[i];
        return total;
    }

    internal int BeginRun()
    {
        data = Load();
        // Overflow in an edited save starts a fresh sequence. Ordinary play cannot reach this boundary.
        data.run = data.run == int.MaxValue ? 1 : data.run + 1;
        data.awardedMask = 0;
        Persist();
        return data.run;
    }

    internal int Credit(int run, int earnedMask)
    {
        if (run <= 0) return 0;
        data = Load();
        if (run != data.run) return 0;
        int newAwards = earnedMask & AwardMask & ~data.awardedMask;
        if (newAwards == 0) return 0;
        int amount = Math.Min(RewardTotal(newAwards), int.MaxValue - Balance);
        data.awardedMask |= newAwards;
        data.balance += amount;
        Persist();
        return amount;
    }

    internal int Price(TabletUpgrade upgrade)
    {
        var prices = upgrade == TabletUpgrade.Attack ? AttackPrices : HullPrices;
        int level = upgrade == TabletUpgrade.Attack ? AttackLevel : HullLevel;
        return level >= prices.Length ? 0 : prices[level];
    }

    internal bool Buy(TabletUpgrade upgrade)
    {
        data = Load();
        int price = Price(upgrade);
        if (price <= 0 || Balance < price) return false;
        if (upgrade == TabletUpgrade.Attack) data.attack++;
        else data.hull++;
        data.balance -= price;
        Persist();
        return true;
    }

    Save Load()
    {
        string raw = PlayerPrefs.GetString(key, "");
        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                var saved = JsonUtility.FromJson<Save>(raw);
                if (saved != null && saved.version == Version && saved.balance >= 0 &&
                    saved.attack >= 0 && saved.attack <= AttackPrices.Length &&
                    saved.hull >= 0 && saved.hull <= HullPrices.Length && saved.run >= 0 &&
                    (saved.awardedMask & ~AwardMask) == 0 && (saved.run != 0 || saved.awardedMask == 0))
                    return saved;
            }
            catch (ArgumentException)
            {
                // Leave the profile at its defaults when the saved JSON cannot be read.
            }
        }
        return new Save { version = Version };
    }

    void Persist()
    {
        PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }

    [Serializable]
    sealed class Save
    {
        public int version, balance, attack, hull, run, awardedMask;
    }
}
