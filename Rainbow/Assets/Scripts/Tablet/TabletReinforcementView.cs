using UnityEngine;
using UnityEngine.UI;

internal sealed partial class TabletView
{
    static readonly Color PressureAlert = new Color32(255, 132, 91, 255);
    static readonly Color PressureBackground = new Color32(65, 23, 28, 245);
    RectTransform pressureWarning;
    Text pressureMessage;

    void BuildReinforcementWarning()
    {
        pressureWarning = Rect(arena, "Reinforcement warning", 0, 0, 1, 1);
        var banner = Graphic(pressureWarning, "Warning banner", .16f, .61f, .84f, .74f, PressureBackground).rectTransform;
        Graphic(banner, "Warning accent", 0, .94f, 1, 1, PressureAlert);
        pressureMessage = Label(banner, "Warning message", .04f, .08f, .96f, .87f, 48, TextColor);
        pressureMessage.alignment = TextAnchor.MiddleCenter;

        var left = Graphic(pressureWarning, "Left entry", .025f, .81f, .19f, .91f, PressureBackground).rectTransform;
        var right = Graphic(pressureWarning, "Right entry", .81f, .81f, .975f, .91f, PressureBackground).rectTransform;
        var leftArrow = Label(left, "Left arrow", 0, 0, 1, 1, 58, PressureAlert);
        var rightArrow = Label(right, "Right arrow", 0, 0, 1, 1, 58, PressureAlert);
        leftArrow.alignment = rightArrow.alignment = TextAnchor.MiddleCenter;
        leftArrow.text = "→";
        rightArrow.text = "←";
        pressureWarning.gameObject.SetActive(false);
    }

    void RenderReinforcementWarning(TabletCombat combat)
    {
        bool warning = combat.ReinforcementWarning;
        bool visible = warning || combat.ReinforcementDeploying;
        if (pressureWarning.gameObject.activeSelf != visible)
            pressureWarning.gameObject.SetActive(visible);
        if (!visible) return;

        string message = warning ? "위기 경보 · 연속 실패 2회\n좌·우에서 적 편대 접근" : "적 편대 2배 투입\n좌·우 동시 진입";
        if (pressureMessage.text != message)
            pressureMessage.text = message;
    }
}
