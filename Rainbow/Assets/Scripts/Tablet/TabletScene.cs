using UnityEngine;
using UnityEngine.UI;

internal enum TabletPhase { Preparation, Launching, InFlight, Debrief, Settlement, Hangar }

/// <summary>Opt-in combat adapter on the tablet scene's original puzzle panel.</summary>
public sealed class TabletScene : MonoBehaviour
{
    internal const float PuzzleWidth = .45f;
    internal const float LaunchTime = .55f;
    const float PurchaseLockTime = .25f;
    static readonly string[] ColorNames = { "", "빨강", "주황", "노랑", "초록", "하늘", "파랑", "보라" };
    static readonly string[] AbilityNames = { "", "직선탄", "관통탄", "확산탄", "수리", "유도탄", "쉴드", "폭발탄" };
    static readonly string[] AbilityAssets = { "", "straight", "piercing", "spread", "heal", "homing", "shield", "blast" };
    static readonly string[] GlyphAssets = { "", "straight", "piercing", "spread", "repair", "homing", "shield", "blast" };

    readonly Sprite[] icons = new Sprite[8];
    readonly Sprite[] glyphs = new Sprite[8];
    TabletView view;
    TabletProgress progress;
    Game puzzle;
    int run, seenAwards;
    float bossDebriefUntil, purchaseLockedUntil;
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
    ScreenOrientation originalOrientation;
#endif
    internal TabletCombat Combat { get; } = new TabletCombat();
    internal TabletPhase Phase { get; private set; }
    internal bool CanLaunch => Phase == TabletPhase.Preparation || Phase == TabletPhase.Hangar;

    internal void Setup(Game game, MatchFeedbackPanel feedback)
    {
        puzzle = game;
        if (progress == null) progress = new TabletProgress();
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
        originalOrientation = Screen.orientation;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
        for (int i = 1; i < icons.Length; i++)
        {
            icons[i] = LoadSprite(AbilityAssets[i]);
            glyphs[i] = LoadSprite("effect-" + GlyphAssets[i]);
        }
        feedback.UseTablet(this);
        var background = GetComponent<Image>();
        background.color = new Color32(18, 29, 48, 255);
        background.sprite = null;
        background.raycastTarget = false;
        // The portrait scene already provides a full-panel background above this image.
        var back = transform.Find("Back").GetComponent<Image>();
        back.sprite = null;
        back.color = background.color;
        back.raycastTarget = false;
        var canvas = GetComponentInParent<Canvas>();
        view = new TabletView(canvas, feedback.DisplayFont, icons, glyphs,
            () => Buy(TabletUpgrade.Attack), () => Buy(TabletUpgrade.Hull));
        Combat.Reset(progress.MaxHp, progress.Power);
        view.ConfigureUpgrades(progress.AttackLevel, progress.HullLevel);
        view.Render(Combat);
        RenderPhase();
    }

    internal Sprite[] TileGlyphs => glyphs;
    internal Sprite GetTileGlyph(int color) => glyphs[color];
    internal static string AbilityName(int color) => AbilityNames[color];

    internal void ResetRound()
    {
        run = progress.BeginRun();
        seenAwards = 0;
        bossDebriefUntil = 0;
        Combat.Reset(progress.MaxHp, progress.Power);
        view.ClearSortieEffects();
        view.ConfigureUpgrades(progress.AttackLevel, progress.HullLevel);
        Phase = TabletPhase.Launching;
        view.Render(Combat);
        RenderPhase();
    }

    internal void StartFlight()
    {
        Phase = TabletPhase.InFlight;
        RenderPhase();
    }

    internal void Tick(float seconds)
    {
        Combat.Advance(seconds);
        int newAwards = Combat.AwardedFlights & ~seenAwards;
        if (newAwards != 0)
        {
            int amount = progress.Credit(run, Combat.AwardedFlights);
            bool boss = (newAwards & TabletProgress.BossBit) != 0;
            seenAwards |= newAwards;
            if (boss) bossDebriefUntil = Time.unscaledTime + TabletView.BossBurstTime;
            view.NoticeReward(amount, boss);
        }
        view.Render(Combat);
    }

    internal void FinishFlight()
    {
        if (Time.unscaledTime >= bossDebriefUntil)
        {
            EnterSettlement();
            return;
        }
        Phase = TabletPhase.Debrief;
        RenderPhase();
    }

    void EnterSettlement()
    {
        view.ClearSortieEffects();
        Phase = TabletPhase.Settlement;
        RenderPhase();
    }

    void EnterHangar()
    {
        if (Phase != TabletPhase.Settlement) return;
        purchaseLockedUntil = 0;
        Phase = TabletPhase.Hangar;
        RenderPhase();
    }

    void Buy(TabletUpgrade upgrade)
    {
        if (Phase != TabletPhase.Hangar || purchaseLockedUntil > 0 || !progress.Buy(upgrade)) return;
        purchaseLockedUntil = Time.unscaledTime + PurchaseLockTime;
        RenderPhase();
    }

    void RenderPhase()
    {
        System.Action action = null;
        if (CanLaunch) action = puzzle.OnClickRetry;
        else if (Phase == TabletPhase.Settlement) action = EnterHangar;
        view.ShowSortie(Phase, Combat, progress, action, purchaseLockedUntil == 0);
    }

    internal string Match(int color, int count)
    {
        int before = Combat.Hp;
        Combat.ApplyMatch(color, count);
        view.Render(Combat);
        if (color == 4)
            return Combat.Hp == before
                ? $"초록 {count}개 · 체력 최대"
                : $"초록 {count}개 · 체력 +{Combat.Hp - before}";
        if (color == 6)
            return $"파랑 {count}개 · 쉴드 {Combat.Shield:0.##}초";
        return $"{ColorNames[color]} {count}개 · {AbilityNames[color]} {count}발";
    }

    void LateUpdate()
    {
        view?.FitSafeArea();
        if (Phase == TabletPhase.Hangar && purchaseLockedUntil > 0 && Time.unscaledTime >= purchaseLockedUntil)
        {
            purchaseLockedUntil = 0;
            RenderPhase();
        }
        if (Phase != TabletPhase.Debrief) return;
        view.Render(Combat);
        if (Time.unscaledTime < bossDebriefUntil) return;
        EnterSettlement();
    }

    void OnDestroy()
    {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
        Screen.orientation = originalOrientation;
#endif
        view?.Dispose();
    }

    internal static Sprite LoadSprite(string name) => Resources.Load<Sprite>("Tablet/" + name);
}
