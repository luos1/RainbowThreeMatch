using UnityEngine;
using UnityEngine.UI;

internal sealed partial class TabletView
{
    static readonly Color GunnerColor = new Color32(255, 167, 87, 255);
    Text enemyForecast;

    void BuildEnemyForecast(RectTransform hud)
    {
        enemyForecast = Label(hud, "Enemy attack forecast", .32f, .58f, .96f, .92f, 38, TextColor);
        enemyForecast.alignment = TextAnchor.MiddleRight;
        enemyForecast.gameObject.SetActive(false);
    }

    void RenderEnemyForecast(TabletCombat state)
    {
        string message = null;
        Color tint = Secondary;
        if (state.Result == TabletResult.Playing)
        {
            var approach = state.NextHeavyShellApproach();
            var attack = state.NextEnemyAttack();
            bool canRepair = state.PaletteSize >= 4 && state.Hp < state.Hull;
            if (approach.HasValue)
            {
                bool covered = state.Shield > approach.Value;
                string response = covered ? "쉴드 전개" : state.PaletteSize >= 6 ? "파랑 쉴드" : $"피해 {TabletCombat.GunnerDamage}";
                message = $"포격탄 {approach.Value:0.0}초 · {response}";
                tint = covered ? ImpactColors[6] : GunnerColor;
            }
            else if (attack.Index >= 0 && state.Enemies[attack.Index].Role == TabletEnemyRole.Gunner)
            {
                message = $"포격기 발사 {attack.Seconds:0.0}초 · 주황 관통탄";
                tint = GunnerColor;
            }
            else if (attack.Index >= 0)
            {
                string response = canRepair ? " · 초록 수리" : "";
                message = $"정찰기 사격 {attack.Seconds:0.0}초{response}";
            }
            else if (canRepair)
            {
                message = "선체 손상 · 초록 수리";
                tint = ImpactColors[4];
            }
        }

        bool shown = message != null;
        if (enemyForecast.gameObject.activeSelf != shown)
            enemyForecast.gameObject.SetActive(shown);
        if (!shown) return;
        enemyForecast.text = message;
        enemyForecast.color = tint;
    }
}
