using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchFeedbackPanel : MonoBehaviour
{
    static readonly string[] ColorNames = { "", "빨강", "주황", "노랑", "초록", "하늘", "파랑", "보라" };
    static readonly Color PanelColor = new Color32(23, 30, 49, 255);
    static readonly Color SecondaryText = new Color32(210, 222, 244, 255);
    static readonly Color HitColor = new Color32(255, 204, 107, 255);
    static readonly Color SourceColor = new Color32(140, 192, 255, 255);
    static readonly Color DestinationColor = new Color32(136, 231, 182, 255);

    [SerializeField] RectTransform board;
    [SerializeField] RectTransform hud;
    [SerializeField] GameObject oldPalette;
    [SerializeField] Font koreanFont;

    readonly Swatch[] swatches = new Swatch[7];
    readonly Image[] abilityIcons = new Image[7];
    Image statusGlyph;
    TabletScene tablet;
    internal Font DisplayFont => koreanFont;
    internal void UseTablet(TabletScene mode) => tablet = mode;
    internal void ShowTabletResult(string message, int color)
    {
        status.text = message;
        SetStatusGlyph(color);
    }

    void SetStatusGlyph(int color)
    {
        if (statusGlyph == null) return;
        statusGlyph.enabled = color > 0;
        if (color > 0) statusGlyph.sprite = tablet.GetTileGlyph(color);
        RectTransform rect = status.rectTransform;
        rect.anchorMin = new Vector2(color > 0 ? .12f : .05f, rect.anchorMin.y);
    }

    internal void SetPalette(int count)
    {
        activeColors = count;
        RefreshPalette();
    }

    readonly Text[] arrows = new Text[6];
    readonly int[] transitions = new int[8];
    readonly bool[] destinations = new bool[8];
    RectTransform panel;
    Text palette;
    Text status;
    Text detail;
    Text guide, pressure;
    (int active, int seconds, int supply, bool preview, int unlocked, int misses, int pending, bool arrived) lastGuide;
    Canvas canvas;
    MatchBurst matchBurst;
    Vector2 boardSize;
    Rect previousSafe;
    int activeColors;
    int matchColor;

    internal void ResetRound(int colors)
    {
        if (panel == null)
            Build();
        activeColors = colors;
        matchColor = 0;
        ClearTransitions();
        status.color = Color.white;
        status.text = "같은 색 3개부터 터져요";
        SetStatusGlyph(0);
        detail.text = "맞닿은 칸은 다음 색으로 바뀌어요";
        if (tablet != null)
        {
            lastGuide = default;
            UpdateTabletBoardGuidance(0, 0, false, 0, 0, 0, false);
        }
        RefreshPalette();
        RefreshMarks(false);
    }

    internal void ShowFailure(int color, int connected)
    {
        matchColor = color;
        ClearTransitions();
        status.color = HitColor;
        status.text = $"{ColorNames[color]} {connected}개만 연결됐어요";
        SetStatusGlyph(0);
        detail.text = "같은 색이 3개 이상 이어진 곳을 눌러요";
        RefreshMarks(true);
    }

    internal void ShowTabletMiss(int color, int connected, TabletMissOutcome outcome)
    {
        ShowFailure(color, connected);
        switch (outcome)
        {
            case TabletMissOutcome.Queued:
                detail.text = "3회 실패 · 다음 소형 편대 2배";
                break;
            case TabletMissOutcome.SupportQueued:
                detail.text = "3회 실패 · 소형 지원편대 출동";
                break;
            case TabletMissOutcome.AlreadyQueued:
                detail.text = "3회 실패 · 이미 증원 대기 중";
                break;
            case TabletMissOutcome.SupportUsed:
                detail.text = "3회 실패 · 마지막 지원편대 사용 완료";
                break;
            default:
                detail.text = $"실패 {tablet.Combat.FailureStreak}/3 · 세 번이면 증원";
                break;
        }
    }

    internal void ShowTabletNoMoves()
    {
        matchColor = 0;
        ClearTransitions();
        SetStatusGlyph(0);
        status.color = SecondaryText;
        status.text = "연결할 칸이 없어 보드를 재배치해요";
        detail.text = "실패 횟수는 그대로예요";
        RefreshMarks(false);
    }

    internal void UpdateTabletBoardGuidance(float elapsed, int supply, bool preview, int unlocked,
        int misses, int pending, bool arrived)
    {
        int next = activeColors < 7 ? activeColors + 1 : 0;
        int seconds = next == 0 ? 0 : Mathf.Max(0, Mathf.CeilToInt(TabletCombat.UnlockSecond(next) - elapsed));
        var current = (activeColors, seconds, supply, preview, unlocked, misses, pending, arrived);
        if (current == lastGuide) return;
        lastGuide = current;

        if (supply > 0)
            guide.text = preview ? $"{ColorNames[supply]} 해금 · 표시 칸 준비"
                : $"{ColorNames[supply]} {TabletScene.AbilityName(supply)} · 표시된 3칸";
        else if (unlocked > 0)
            guide.text = $"{ColorNames[unlocked]} {TabletScene.AbilityName(unlocked)} 해금";
        else if (next > 0)
            guide.text = seconds == 0 ? $"보충 뒤 {ColorNames[next]} 해금"
                : $"{seconds}초 후 {ColorNames[next]} {TabletScene.AbilityName(next)}";
        else
            guide.text = "상하좌우 같은 색 3칸";

        pressure.color = pending > 0 || arrived || misses > 0 ? HitColor : SecondaryText;
        pressure.text = pending > 0 ? $"증원 +{pending} · {misses}/3"
            : arrived ? $"증원 도착 · {misses}/3" : $"실패 {misses}/3";
    }

    internal void ClearTabletGuidance()
    {
        if (pressure == null) return;
        pressure.text = "";
        guide.text = "상하좌우 같은 색 3칸";
        lastGuide = default;
    }

    internal void StartMatch(int color, int count)
    {
        matchColor = color;
        ClearTransitions();
        status.color = HitColor;
        status.text = $"{ColorNames[color]} {count}개 매치!";
        detail.text = "이웃 색을 바꾸는 중";
        RefreshMarks(false);
    }

    internal void RecordTransition(int before, int after)
    {
        transitions[before] = after;
        destinations[after] = true;
    }

    internal void FinishMatch(int neighborCount, int newPaletteSize)
    {
        activeColors = newPaletteSize;
        RefreshPalette();
        RefreshMarks(true);
        if (neighborCount == 0)
        {
            detail.text = "보드를 모두 비웠어요";
            return;
        }

        var message = new StringBuilder();
        message.Append($"이웃 {neighborCount}칸: ");
        int changes = 0;
        for (int before = 1; before < transitions.Length; before++)
        {
            int after = transitions[before];
            if (after == 0)
                continue;
            if (changes == 1)
                message.Append(" · ");
            if (changes < 2)
                message.Append($"{ColorNames[before]} → {ColorNames[after]}");
            changes++;
        }
        if (changes > 2)
            message.Append($" 외 {changes - 2}가지");
        detail.text = message.ToString();
    }

    void ClearTransitions()
    {
        System.Array.Clear(transitions, 0, transitions.Length);
        System.Array.Clear(destinations, 0, destinations.Length);
    }

    void RefreshPalette()
    {
        palette.text = $"{activeColors}/7색 · {ColorNames[activeColors]} → 빨강";
        for (int i = 0; i < swatches.Length; i++)
        {
            bool available = i < activeColors;
            swatches[i].Icon.color = tablet == null
                ? new Color(1f, 1f, 1f, available ? 1f : .38f)
                : TabletTileArt.Background(i + 1, available);
            swatches[i].Name.color = available ? Color.white : SecondaryText;
            if (abilityIcons[i] != null)
                abilityIcons[i].color = new Color(1, 1, 1, available ? 1f : .3f);
            if (i < arrows.Length)
                arrows[i].color = i + 1 < activeColors ? SecondaryText : new Color32(146, 163, 193, 255);
        }
    }

    void RefreshMarks(bool pulse)
    {
        for (int i = 0; i < swatches.Length; i++)
        {
            var swatch = swatches[i];
            int index = i + 1;
            bool hit = index == matchColor;
            bool source = transitions[index] != 0;
            bool destination = destinations[index];
            swatch.Frame.enabled = hit;
            swatch.From.enabled = source;
            swatch.To.enabled = destination;
            var icon = swatch.Icon.rectTransform;
            icon.DOKill();
            icon.localScale = Vector3.one;
            if (pulse && (hit || source || destination))
                icon.DOPunchScale(Vector3.one * .15f, .25f, 3, .45f);
        }
    }

    void OnDisable() => StopPulse();

    internal void StopPulse()
    {
        foreach (var swatch in swatches)
        {
            if (swatch != null && swatch.Icon != null)
            {
                swatch.Icon.rectTransform.DOKill();
                swatch.Icon.rectTransform.localScale = Vector3.one;
            }
        }
    }

    void Build()
    {
        canvas = GetComponentInParent<Canvas>();
        matchBurst = GetComponent<MatchBurst>();
        FitBoardTouchTargets();
        if (tablet != null)
        {
            if (hud.TryGetComponent<Image>(out var hudImage))
                hudImage.color = PanelColor;
            if (board.TryGetComponent<Image>(out var tabletBoard))
                tabletBoard.color = TabletTileArt.Empty;
        }
        oldPalette.SetActive(false);
        panel = RectFor(transform, "Match Feedback", new Vector2(0f, 0f), Vector2.one);
        panel.SetSiblingIndex(board.GetSiblingIndex() + 1);
        var background = panel.gameObject.AddComponent<Image>();
        background.color = PanelColor;
        background.raycastTarget = false;

        MakeText(panel, "Palette title", "색이 바뀌는 순서", .05f, .81f, .54f, .98f, 43, Color.white, TextAnchor.MiddleLeft);
        palette = MakeText(panel, "Palette wrap", "", .53f, .81f, .95f, .98f, 36, SecondaryText, TextAnchor.MiddleRight);
        status = MakeText(panel, "Match status", "", .05f, .335f, .95f, .485f, 44, Color.white, TextAnchor.MiddleLeft);
        if (tablet != null)
        {
            statusGlyph = MakeImage(panel, "Matched effect", .05f, .335f, .102f, .485f, Color.white);
            statusGlyph.preserveAspect = true;
        }
        detail = MakeText(panel, "Match detail", "", .05f, .19f, .95f, .34f, 36, SecondaryText, TextAnchor.MiddleLeft);
        guide = MakeText(panel, "Match rule", "상하좌우로 이어진 같은 색을 눌러요",
            .05f, .035f, tablet != null ? .66f : .95f, .17f, 34, SecondaryText, TextAnchor.MiddleLeft);
        if (tablet != null)
            pressure = MakeText(panel, "Tablet miss status", "", .66f, .035f, .95f, .17f,
                34, SecondaryText, TextAnchor.MiddleRight);

        const float left = .045f;
        const float slotWidth = .91f / 7f;
        for (int i = 0; i < swatches.Length; i++)
        {
            float x = left + i * slotWidth;
            var slot = RectFor(panel, $"Color {i + 1}", new Vector2(x + .01f, .49f), new Vector2(x + slotWidth - .01f, .81f));
            var frame = MakeImage(slot, "Matched color", .09f, .32f, .91f, .945f, HitColor);
            var icon = MakeImage(slot, "Color icon", .15f, .36f, .85f, .9f, Color.white);
            icon.preserveAspect = true;
            if (tablet == null)
                icon.sprite = ColorHelper.Instance.GetPaletteSprite(i);
            else
            {
                var ability = TabletTileArt.AddGlyph(icon.rectTransform);
                ability.sprite = tablet.GetTileGlyph(i + 1);
                abilityIcons[i] = ability;
            }
            var from = MakeImage(slot, "Previous color", .15f, .91f, .85f, .96f, SourceColor);
            var to = MakeImage(slot, "Next color", .15f, .30f, .85f, .345f, DestinationColor);
            var label = MakeText(slot, "Color name", ColorNames[i + 1], 0f, 0f, 1f, .305f, 35, Color.white, TextAnchor.MiddleCenter);
            swatches[i] = new Swatch(icon, label, frame, from, to);
            if (i < arrows.Length)
            {
                float between = x + slotWidth;
                arrows[i] = MakeText(panel, $"Arrow {i + 1}", "→", between - .027f, .58f, between + .027f, .775f,
                    34, SecondaryText, TextAnchor.MiddleCenter);
            }
        }

        UpdateHud(hud.GetComponentInChildren<ScoreText>().GetComponent<TMP_Text>(), 0);
        UpdateHud(hud.GetComponentInChildren<GradeText>().GetComponent<TMP_Text>(), 1);
        UpdateHud(hud.GetComponentInChildren<TimerText>().GetComponent<TMP_Text>(), 2);
        Layout();
    }

    void FitBoardTouchTargets()
    {
        const float touchPadding = 5f;
        Vector2 low = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 high = new Vector2(float.MinValue, float.MinValue);
        foreach (var column in GetComponent<Game>().blocks)
        foreach (var block in column.blocks)
        {
            var graphic = block.GetComponent<Button>().targetGraphic;
            graphic.raycastPadding = Vector4.one * -touchPadding;
            var rect = graphic.rectTransform;
            low = Vector2.Min(low, board.InverseTransformPoint(rect.TransformPoint(rect.rect.min)));
            high = Vector2.Max(high, board.InverseTransformPoint(rect.TransformPoint(rect.rect.max)));
        }
        boardSize = high - low + Vector2.one * (2f * touchPadding);
    }

    void UpdateHud(TMP_Text label, int column)
    {
        var rect = label.rectTransform;
        rect.SetParent(hud, false);
        rect.anchorMin = new Vector2(column / 3f, 0f);
        rect.anchorMax = new Vector2((column + 1) / 3f, 1f);
        rect.offsetMin = new Vector2(10f, 0f);
        rect.offsetMax = new Vector2(-10f, 0f);
        rect.localScale = Vector3.one;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSizeMin = 32f;
        label.fontSizeMax = 44f;
        label.raycastTarget = false;
        if (tablet != null)
            label.color = Color.white;
    }

    void LateUpdate()
    {
        if (panel != null)
            Layout();
    }

    void Layout()
    {
        var canvasRect = (RectTransform)canvas.transform;
        var screen = Screen.safeArea;
        if (screen.width <= 0f || screen.height <= 0f)
            return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen.min, canvas.worldCamera, out var low);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen.max, canvas.worldCamera, out var high);
        var safe = Rect.MinMaxRect(low.x, low.y, high.x, high.y);
        if (tablet != null)
            safe.width *= TabletScene.PuzzleWidth;
        if (safe == previousSafe || safe.width < 1f || safe.height < 1f)
            return;
        previousSafe = safe;
        var fit = Fit(safe, boardSize);
        if (matchBurst != null)
            matchBurst.Clear();
        board.localScale = Vector3.one * fit.scale;
        board.position = canvasRect.TransformPoint(fit.center);
        Place(hud, fit.hud, canvasRect);
        Place(panel, fit.panel, canvasRect);
    }

    internal static (Rect hud, Rect panel, Vector2 center, float scale) Fit(Rect safe, Vector2 boardSize)
    {
        float margin = Mathf.Min(30f, Mathf.Min(safe.width, safe.height) * .035f);
        float width = Mathf.Min(1020f, safe.width - 2f * margin);
        float height = safe.height - 2f * margin;
        float hudHeight = Mathf.Min(106f, height * .075f);
        float panelHeight = Mathf.Min(356f, height * .25f);
        float gap = Mathf.Min(22f, height * .02f);
        float left = safe.center.x - width * .5f;
        var hud = new Rect(left, safe.yMax - margin - hudHeight, width, hudHeight);
        var panel = new Rect(left, safe.yMin + margin, width, panelHeight);
        float boardBottom = panel.yMax + gap;
        float boardTop = hud.yMin - gap;
        float scale = Mathf.Min(1.14f, width / boardSize.x, (boardTop - boardBottom) / boardSize.y);
        return (hud, panel, new Vector2(safe.center.x, (boardBottom + boardTop) * .5f), scale);
    }

    static void Place(RectTransform target, Rect area, RectTransform canvasRect)
    {
        var parent = (RectTransform)target.parent;
        target.anchorMin = target.anchorMax = parent.pivot;
        target.pivot = new Vector2(.5f, .5f);
        target.sizeDelta = area.size;
        target.position = canvasRect.TransformPoint(area.center);
    }

    Text MakeText(RectTransform parent, string name, string value, float left, float bottom, float right, float top,
        int size, Color color, TextAnchor alignment)
    {
        var rect = RectFor(parent, name, new Vector2(left, bottom), new Vector2(right, top));
        var text = rect.gameObject.AddComponent<Text>();
        text.font = koreanFont;
        text.text = value;
        text.color = color;
        text.alignment = alignment;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 24;
        text.resizeTextMaxSize = size;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static Image MakeImage(RectTransform parent, string name, float left, float bottom, float right, float top, Color color)
    {
        var image = RectFor(parent, name, new Vector2(left, bottom), new Vector2(right, top)).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static RectTransform RectFor(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.layer = parent.gameObject.layer;
        var rect = (RectTransform)item.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    sealed class Swatch
    {
        internal readonly Image Icon, Frame, From, To;
        internal readonly Text Name;

        internal Swatch(Image icon, Text name, Image frame, Image from, Image to)
        {
            Icon = icon;
            Name = name;
            Frame = frame;
            From = from;
            To = to;
        }
    }
}
