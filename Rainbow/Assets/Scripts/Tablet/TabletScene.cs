using UnityEngine;
using UnityEngine.UI;

/// <summary>Opt-in combat adapter on the tablet scene's original puzzle panel.</summary>
public sealed class TabletScene : MonoBehaviour
{
    internal const float PuzzleWidth = .45f;
    static readonly string[] ColorNames = { "", "빨강", "주황", "노랑", "초록", "하늘", "파랑", "보라" };
    static readonly string[] AbilityNames = { "", "직선탄", "관통탄", "확산탄", "", "유도탄", "", "폭발탄" };
    static readonly string[] AbilityAssets = { "", "straight", "piercing", "spread", "heal", "homing", "shield", "blast" };

    readonly Sprite[] icons = new Sprite[8];
    TabletView view;
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
    ScreenOrientation originalOrientation;
#endif
    internal TabletCombat Combat { get; } = new TabletCombat();

    internal void Setup(Game game, MatchFeedbackPanel feedback)
    {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
        originalOrientation = Screen.orientation;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
        for (int i = 1; i < icons.Length; i++)
            icons[i] = LoadSprite(AbilityAssets[i]);
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
        view = new TabletView(canvas, feedback.DisplayFont, game.OnClickRetry, icons);
    }

    internal Sprite GetAbilityIcon(int color) => icons[color];

    internal void ResetRound()
    {
        Combat.Reset();
        view.Render(Combat);
    }

    internal void Tick(float seconds)
    {
        Combat.Advance(seconds);
        view.Render(Combat);
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

    void LateUpdate() => view?.FitSafeArea();

    void OnDestroy()
    {
#if !UNITY_EDITOR && (UNITY_IOS || UNITY_ANDROID)
        Screen.orientation = originalOrientation;
#endif
        view?.Dispose();
    }

    internal static Sprite LoadSprite(string name) => Resources.Load<Sprite>("Tablet/" + name);
}
