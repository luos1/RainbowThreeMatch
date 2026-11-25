using System;
using UnityEngine;
using UnityEngine.UI;

internal sealed partial class TabletView
{
    internal const float BossBurstTime = .57f;
    static readonly Color RewardGold = new Color32(255, 210, 121, 255);
    RectTransform sortiePanel, card, bossPanel, rewardPanel, upgradeCards;
    Text cardTitle, cardDetail, primaryText, attackText, hullText, bossText, rewardText;
    Button primaryButton, attackButton, hullButton;
    Image bossFill, bossBurst;
    Action primaryAction;
    float noticeUntil, bossBurstUntil;

    void BuildSortieFlow(Action attack, Action hull)
    {
        bossPanel = Graphic(arena, "Boss status", .07f, .925f, .93f, .986f, Hull).rectTransform;
        bossText = Label(bossPanel, "Boss title", .05f, .24f, .95f, 1, 34, RewardGold);
        var health = Graphic(bossPanel, "Boss health", .05f, .08f, .95f, .25f, Border).rectTransform;
        bossFill = Graphic(health, "Boss health fill", 0, 0, 1, 1, EnemyHealth);
        bossBurst = Entity("Boss defeat burst", explosions[0]);
        rewardPanel = Graphic(arena, "Reward banner", .32f, .854f, .68f, .918f,
            new Color32(19, 29, 44, 242)).rectTransform;
        rewardText = Label(rewardPanel, "Reward notice", .03f, .04f, .97f, .96f, 42, RewardGold);
        rewardText.alignment = TextAnchor.MiddleCenter;
        bossPanel.gameObject.SetActive(false);
        rewardPanel.gameObject.SetActive(false);
        bossBurst.gameObject.SetActive(false);

        var scrim = Graphic(arena, "Sortie overlay", 0, 0, 1, 1, new Color32(7, 16, 31, 214));
        scrim.raycastTarget = true;
        sortiePanel = scrim.rectTransform;
        card = Graphic(sortiePanel, "Sortie panel", .08f, .15f, .92f, .85f, Hull).rectTransform;
        Graphic(card, "Accent", .08f, .89f, .24f, .90f, Accent);
        cardTitle = Label(card, "Sortie title", .08f, .72f, .94f, .88f, 66, TextColor);
        cardDetail = Label(card, "Sortie detail", .08f, .55f, .93f, .72f, 38, Secondary);

        upgradeCards = Rect(card, "Ship upgrades", .08f, .25f, .92f, .54f);
        attackButton = CardButton(upgradeCards, "Upgrade Attack", 0, .48f, 0, 1, Border, attack, out attackText);
        hullButton = CardButton(upgradeCards, "Upgrade Hull", .52f, 1, 0, 1, Border, hull, out hullText);

        primaryButton = CardButton(card, "Tablet Sortie", .08f, .63f, .07f, .22f, Accent,
            () => primaryAction?.Invoke(), out primaryText);
        primaryText.color = new Color32(12, 34, 49, 255);
    }

    Button CardButton(Transform parent, string name, float left, float right, float bottom, float top,
        Color color, Action clicked, out Text text)
    {
        var graphic = Graphic(parent, name, left, bottom, right, top, color);
        graphic.raycastTarget = true;
        var button = graphic.gameObject.AddComponent<Button>();
        button.targetGraphic = graphic;
        ColorBlock states = button.colors;
        states.disabledColor = new Color32(140, 150, 167, 255);
        button.colors = states;
        button.onClick.AddListener(() => clicked());
        text = Label(graphic.transform, name + " label", .06f, .05f, .94f, .95f, 38, TextColor);
        text.resizeTextMinSize = 14;
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    internal void ShowSortie(TabletPhase phase, TabletCombat combat, TabletProgress progress, Action action, bool upgradesReady)
    {
        primaryAction = action;
        bool hangar = phase == TabletPhase.Hangar;
        bool visible = phase != TabletPhase.InFlight && phase != TabletPhase.Debrief;
        sortiePanel.gameObject.SetActive(visible);
        if (!visible) return;

        string stats = $"공격력 {progress.Power * 100:0}% · 최대 체력 {progress.MaxHp}";
        upgradeCards.gameObject.SetActive(hangar);
        primaryButton.gameObject.SetActive(action != null);
        primaryButton.interactable = action != null;
        switch (phase)
        {
            case TabletPhase.Preparation:
                cardTitle.text = "훈련 출격 준비";
                cardDetail.text = $"{stats}\n편대가 몰려옵니다. 퍼즐로 모함을 지키세요";
                primaryText.text = "출격";
                break;
            case TabletPhase.Launching:
                cardTitle.text = "발진 중";
                cardDetail.text = stats;
                break;
            case TabletPhase.Settlement:
                cardTitle.text = combat.Result == TabletResult.Failed ? "모함 파괴" :
                    combat.Result == TabletResult.Cleared ? "훈련 목표 달성" : "60초 생존";
                string boss = combat.Boss.Hp <= 0 && combat.Boss.Spawned ? "보스 격파" : "보스 미격파";
                cardDetail.text = $"이번 출격 +{TabletProgress.RewardTotal(combat.AwardedFlights)}   ·   보유 {progress.Balance}\n격파 {combat.EnemiesDefeated}/{combat.PlannedEnemies}   ·   {boss}";
                primaryText.text = "정비하기";
                break;
            case TabletPhase.Hangar:
                cardTitle.text = "격납고";
                cardDetail.text = $"보유 {progress.Balance}   ·   {stats}\n강화된 기체로 같은 훈련에 출격합니다";
                primaryText.text = "다시 출격";
                UpgradeCard(progress, TabletUpgrade.Attack, attackButton, attackText, upgradesReady);
                UpgradeCard(progress, TabletUpgrade.Hull, hullButton, hullText, upgradesReady);
                break;
        }
    }

    static void UpgradeCard(TabletProgress progress, TabletUpgrade upgrade, Button button, Text text, bool upgradesReady)
    {
        bool attack = upgrade == TabletUpgrade.Attack;
        int level = attack ? progress.AttackLevel : progress.HullLevel;
        int price = progress.Price(upgrade);
        string current = attack ? $"{progress.Power * 100:0}%" : $"{progress.MaxHp}";
        string next = attack ? $"{(progress.Power + .1f) * 100:0}%" : $"{progress.MaxHp + 1}";
        text.text = price == 0 ? $"{(attack ? "공격력" : "최대 체력")} Lv.{level}\n{current} · 최대 단계" :
            $"{(attack ? "공격력" : "최대 체력")} Lv.{level}\n{current} → {next}\n비용 {price}";
        button.interactable = upgradesReady && price > 0 && progress.Balance >= price;
    }

    internal void NoticeReward(int amount, bool boss)
    {
        noticeUntil = Time.unscaledTime + 2.25f;
        rewardText.text = boss ? $"보스 격파   +{amount}" : $"편대 격파   +{amount}";
        if (boss) bossBurstUntil = Time.unscaledTime + BossBurstTime;
    }

    internal void ClearSortieEffects()
    {
        noticeUntil = bossBurstUntil = 0;
        rewardPanel.gameObject.SetActive(false);
        bossPanel.gameObject.SetActive(false);
        bossBurst.gameObject.SetActive(false);
    }

    void RenderSortieEffects(TabletCombat combat, float unit)
    {
        var boss = combat.Boss;
        bool warning = combat.BossApproaching;
        bool active = boss.Spawned && boss.Hp > 0 && combat.Result == TabletResult.Playing;
        bool shown = warning || active;
        if (bossPanel.gameObject.activeSelf != shown)
            bossPanel.gameObject.SetActive(shown);
        if (shown)
        {
            string title = active ? $"최종 보스   {boss.Hp:0.#}/{boss.MaxHp:0}" : "최종 보스 접근";
            if (bossText.text != title) bossText.text = title;
            Fill(bossFill, active ? boss.Hp / boss.MaxHp : 0);
        }

        float now = Time.unscaledTime;
        if (rewardPanel.gameObject.activeSelf != (now < noticeUntil))
            rewardPanel.gameObject.SetActive(now < noticeUntil);
        SetActive(bossBurst, now < bossBurstUntil);
        if (now >= bossBurstUntil) return;

        float progress = 1 - (bossBurstUntil - now) / BossBurstTime;
        bossBurst.sprite = explosions[Mathf.Min((int)(progress * explosions.Length), explosions.Length - 1)];
        bossBurst.color = new Color(1f, .86f, .65f, 1 - progress * .7f);
        Place(bossBurst, boss.Position, unit * (.20f + progress * .13f) * weaponFeedback.DefeatScale);
    }
}
