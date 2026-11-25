using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal sealed class TabletView
{
    static readonly Color TextColor = new Color32(234, 242, 255, 255);
    static readonly Color Accent = new Color32(115, 231, 222, 255);
    static readonly Color Secondary = new Color32(166, 187, 211, 255);
    static readonly Color Hull = new Color32(22, 39, 59, 255);
    static readonly Color Border = new Color32(48, 74, 97, 255);

    readonly Font font;
    readonly RectTransform safeRoot, arena, result;
    readonly Image player, playerShield, hpFill, shieldFill;
    readonly Text hpText, shieldText, waveText, shotsText, title, resultStats;
    readonly Image[] enemySprites = new Image[11], warnings = new Image[11], enemyBars = new Image[11];
    readonly Image[] shotSprites = new Image[TabletCombat.ShotCapacity], burstSprites = new Image[10];
    readonly Sprite[] enemyArt = new Sprite[3], explosions = new Sprite[4], abilityArt;
    readonly Sprite ring;
    Rect previousSafe;
    Vector2 previousScreen;
    (int hp, int shield, int wave, int fired, int waiting, int blocked) previousStatus = (-1, -1, -1, -1, -1, -1);
    TabletResult lastResult = TabletResult.Playing;

    internal TabletView(Canvas canvas, Font font, Action retry, Sprite[] abilities)
    {
        this.font = font;
        abilityArt = abilities;
        ring = Art("ring");
        enemyArt[0] = Art("enemy-scout");
        enemyArt[1] = Art("enemy-wing");
        enemyArt[2] = Art("enemy-elite");
        for (int i = 0; i < explosions.Length; i++)
            explosions[i] = Art("burst-" + (i + 1));

        safeRoot = Rect(canvas.transform, "Tablet Safe Area", 0, 0, 1, 1);
        var pane = Rect(safeRoot, "Combat Pane", TabletScene.PuzzleWidth, .026f, .984f, .974f);
        Graphic(pane, "Divider", 0, 0, .004f, 1, Border);
        var hud = Graphic(pane, "Combat HUD", .027f, .874f, 1, 1, Hull).rectTransform;
        waveText = Label(hud, "Wave", .042f, .58f, .96f, .92f, 45, Accent);
        hpText = Label(hud, "HP", .042f, .15f, .26f, .52f, 38, TextColor);
        var hpTrack = Graphic(hud, "Health bar", .25f, .25f, .48f, .39f, new Color32(42, 63, 82, 255));
        hpFill = Graphic(hpTrack.rectTransform, "Fill", 0, 0, 1, 1, new Color32(102, 225, 167, 255));
        shieldText = Label(hud, "Shield", .54f, .15f, .80f, .52f, 38, TextColor);
        var shieldTrack = Graphic(hud, "Shield bar", .80f, .25f, .96f, .39f, new Color32(42, 63, 82, 255));
        shieldFill = Graphic(shieldTrack.rectTransform, "Fill", 0, 0, 1, 1, new Color32(102, 171, 255, 255));

        var field = Graphic(pane, "Arena frame", .027f, .081f, 1, .855f, Border).rectTransform;
        arena = Graphic(field, "Arena", .003f, .003f, .997f, .997f, new Color32(5, 10, 25, 255)).rectTransform;
        arena.gameObject.AddComponent<RectMask2D>();
        var background = Graphic(arena, "Generated arena", 0, 0, 1, 1, Color.white);
        background.sprite = Art("arena");

        playerShield = Entity("Player shield", ring);
        playerShield.color = new Color(.6f, .86f, 1f, .78f);
        player = Entity("Player ship", Art("player"));
        for (int i = 0; i < enemySprites.Length; i++)
        {
            warnings[i] = Entity("Enemy strike warning " + (i + 1), ring);
            warnings[i].color = new Color(1, .45f, .25f, .85f);
            enemySprites[i] = Entity("Enemy " + (i + 1), enemyArt[0]);
            enemyBars[i] = Graphic(arena, "Enemy HP " + (i + 1), 0, 0, 0, 0, new Color32(255, 147, 110, 255));
        }
        for (int i = 0; i < shotSprites.Length; i++)
            shotSprites[i] = Entity("Shot " + (i + 1), abilityArt[1]);
        for (int i = 0; i < burstSprites.Length; i++)
            burstSprites[i] = Entity("Burst " + (i + 1), explosions[0]);

        var footer = Graphic(pane, "Combat status", .027f, 0, 1, .064f, Hull).rectTransform;
        shotsText = Label(footer, "Shot totals", .043f, .13f, .96f, .86f, 37, Secondary);

        result = Graphic(arena, "Result overlay", 0, 0, 1, 1, new Color32(7, 16, 31, 220)).rectTransform;
        var recap = Graphic(result, "Round result", .115f, .25f, .885f, .75f, Hull).rectTransform;
        Graphic(recap, "Accent", .08f, .87f, .22f, .89f, Accent);
        title = Label(recap, "Result title", .08f, .60f, .92f, .84f, 75, TextColor);
        resultStats = Label(recap, "Result detail", .08f, .42f, .92f, .62f, 38, Secondary);
        var buttonGraphic = Graphic(recap, "Tablet Retry", .08f, .14f, .53f, .37f, Accent);
        buttonGraphic.raycastTarget = true;
        var button = buttonGraphic.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonGraphic;
        button.onClick.AddListener(() => retry());
        var buttonText = Label(button.transform, "Retry label", 0, 0, 1, 1, 44, new Color32(12, 34, 49, 255));
        buttonText.alignment = TextAnchor.MiddleCenter;
        result.gameObject.SetActive(false);
        FitSafeArea();
    }

    internal void FitSafeArea()
    {
        float width = Screen.width, height = Screen.height;
        Rect safe = Screen.safeArea;
        if (width <= 0 || height <= 0 || safe.width <= 0 || safe.height <= 0)
            return;
        var size = new Vector2(width, height);
        if (safe == previousSafe && size == previousScreen)
            return;
        previousSafe = safe;
        previousScreen = size;
        safeRoot.anchorMin = new Vector2(safe.xMin / width, safe.yMin / height);
        safeRoot.anchorMax = new Vector2(safe.xMax / width, safe.yMax / height);
    }

    internal void Render(TabletCombat state)
    {
        FitSafeArea();
        int shieldTenths = Mathf.CeilToInt(state.Shield * 10);
        var status = (state.Hp, shieldTenths, state.Wave, state.FiredShots, state.PendingShots, state.BlockedDamage);
        if (status != previousStatus)
        {
            previousStatus = status;
            waveText.text = $"웨이브  {state.Wave} / 3";
            hpText.text = $"체력 {state.Hp}/{TabletCombat.MaxHp}";
            shieldText.text = $"쉴드 {shieldTenths / 10f:0.0}초";
            Fill(hpFill, state.Hp / (float)TabletCombat.MaxHp);
            Fill(shieldFill, state.Shield / TabletCombat.MaxShield);
            shotsText.text = $"발사 {state.FiredShots}발   ·   대기 {state.PendingShots}발   ·   방어 {state.BlockedDamage}";
        }

        float unit = Mathf.Min(arena.rect.width, arena.rect.height);
        Place(player, state.Player, unit * .138f);
        Direction(player, Vector2.up);
        SetActive(playerShield, state.Shield > 0);
        if (state.Shield > 0)
            Place(playerShield, state.Player, unit * .205f);

        for (int i = 0; i < enemySprites.Length; i++)
        {
            var enemy = state.Enemies[i];
            bool visible = enemy.Spawned && enemy.Hp > 0;
            SetActive(enemySprites[i], visible);
            SetActive(enemyBars[i], visible);
            SetActive(warnings[i], visible && enemy.Warning && state.Result == TabletResult.Playing);
            if (!visible) continue;
            float shipSize = unit * (enemy.Style == 2 ? .171f : enemy.Style == 1 ? .133f : .107f);
            Place(warnings[i], enemy.Position, shipSize * 1.4f);
            enemySprites[i].sprite = enemyArt[enemy.Style];
            Place(enemySprites[i], enemy.Position, shipSize);
            Place(enemyBars[i], enemy.Position + Vector2.up * .075f, shipSize * .6f);
            enemyBars[i].rectTransform.sizeDelta = new Vector2(shipSize * .6f * (enemy.Hp / enemy.MaxHp), Mathf.Max(3, unit * .004f));
        }

        for (int i = 0; i < shotSprites.Length; i++)
        {
            var shot = state.Shots[i];
            SetActive(shotSprites[i], shot.Active);
            if (!shot.Active) continue;
            shotSprites[i].sprite = abilityArt[shot.Color];
            Place(shotSprites[i], shot.Position, unit * .085f);
            Direction(shotSprites[i], shot.Direction);
        }

        for (int i = 0; i < burstSprites.Length; i++)
        {
            var fx = state.Effects[i];
            SetActive(burstSprites[i], fx.Lifetime > 0);
            if (fx.Lifetime <= 0) continue;
            int frame = Mathf.Clamp(Mathf.FloorToInt((1 - fx.Lifetime / .32f) * 4), 0, 3);
            burstSprites[i].sprite = explosions[frame];
            Place(burstSprites[i], fx.Position, unit * fx.Size);
        }

        if (state.Result == lastResult)
            return;
        lastResult = state.Result;
        result.gameObject.SetActive(lastResult != TabletResult.Playing);
        if (lastResult == TabletResult.Playing)
            return;
        title.text = lastResult == TabletResult.Failed ? "기체 파괴" :
            lastResult == TabletResult.Cleared ? "전 웨이브 격파" : "생존 성공";
        resultStats.text = $"격파 {state.EnemiesDefeated}/11   ·   체력 {state.Hp}   ·   명중 {state.HitShots}/{state.FiredShots}";
    }

    internal void Dispose()
    {
        if (safeRoot != null)
            Object.Destroy(safeRoot.gameObject);
    }

    static void Fill(Image image, float value) => image.rectTransform.anchorMax = new Vector2(value, 1);
    static Sprite Art(string name) => TabletScene.LoadSprite(name);

    static void SetActive(Image image, bool active)
    {
        if (image.gameObject.activeSelf != active)
            image.gameObject.SetActive(active);
    }

    static void Place(Image image, Vector2 normalized, float size)
    {
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = normalized;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.one * size;
    }

    static void Direction(Image image, Vector2 direction)
    {
        image.rectTransform.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, direction));
    }

    Image Entity(string name, Sprite sprite)
    {
        var image = Graphic(arena, name, 0, 0, 0, 0, Color.white);
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    Text Label(Transform parent, string name, float left, float bottom, float right, float top, int size, Color color)
    {
        var text = Rect(parent, name, left, bottom, right, top).gameObject.AddComponent<Text>();
        text.font = font;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 18;
        text.resizeTextMaxSize = size;
        text.raycastTarget = false;
        return text;
    }

    static Image Graphic(Transform parent, string name, float left, float bottom, float right, float top, Color color)
    {
        var image = Rect(parent, name, left, bottom, right, top).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static RectTransform Rect(Transform parent, string name, float left, float bottom, float right, float top)
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.layer = parent.gameObject.layer;
        var rect = (RectTransform)item.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
}
