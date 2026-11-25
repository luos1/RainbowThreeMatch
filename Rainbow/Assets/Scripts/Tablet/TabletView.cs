using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal sealed partial class TabletView
{
    static readonly Color TextColor = new Color32(234, 242, 255, 255);
    static readonly Color Accent = new Color32(115, 231, 222, 255);
    static readonly Color Secondary = new Color32(166, 187, 211, 255);
    static readonly Color Hull = new Color32(22, 39, 59, 255);
    static readonly Color Border = new Color32(48, 74, 97, 255);
    static readonly Color EnemyHealth = new Color32(255, 147, 110, 255);
    static readonly Color[] ImpactColors =
    {
        Color.white, new Color32(255, 106, 91, 255), new Color32(255, 175, 73, 255),
        new Color32(255, 233, 140, 255), new Color32(116, 255, 169, 255),
        new Color32(100, 224, 255, 255), new Color32(117, 163, 255, 255),
        new Color32(224, 166, 255, 255)
    };
    const float RecoilDistance = .042f;
    const float TrailThreshold = .12f;
    const float BackgroundScroll = .14f;

    readonly Font font;
    readonly RectTransform safeRoot, arena;
    readonly Image player, playerShield, hpFill, shieldFill;
    readonly Text hpText, shieldText, waveText, shotsText;
    readonly Image[] enemySprites = new Image[11], warnings = new Image[11], enemyBars = new Image[11];
    readonly Image[] enemyTrails = new Image[11];
    readonly Image[] backdropTiles = new Image[2];
    readonly TabletStarFlow starFlow;
    readonly Image[] shotSprites = new Image[TabletCombat.ShotCapacity], burstSprites = new Image[TabletCombat.EffectCapacity];
    readonly Sprite[] enemyArt = new Sprite[3], explosions = new Sprite[4], abilityArt;
    readonly Sprite ring;
    Rect previousSafe;
    Vector2 previousScreen;
    int backdropCycle = -1;
    (int hp, int shield, int wave, int fired, int waiting, int blocked) previousStatus = (-1, -1, -1, -1, -1, -1);
    internal TabletView(Canvas canvas, Font font, Sprite[] abilities, Action attack, Action hull)
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
        var backdrop = Art("arena");
        for (int i = 0; i < backdropTiles.Length; i++)
        {
            backdropTiles[i] = Graphic(arena, "Scrolling space " + (i + 1), 0, 0, 1, 1, Color.white);
            backdropTiles[i].sprite = backdrop;
        }
        starFlow = Rect(arena, "Star flow", 0, 0, 1, 1).gameObject.AddComponent<TabletStarFlow>();
        starFlow.raycastTarget = false;

        playerShield = Entity("Player shield", ring);
        playerShield.color = new Color(.6f, .86f, 1f, .78f);
        player = Entity("Player ship", Art("player"));
        for (int i = 0; i < enemySprites.Length; i++)
        {
            warnings[i] = Entity("Enemy strike warning " + (i + 1), ring);
            warnings[i].color = new Color(1, .45f, .25f, .85f);
            enemyTrails[i] = Entity("Enemy trail " + (i + 1), enemyArt[0]);
            enemySprites[i] = Entity("Enemy " + (i + 1), enemyArt[0]);
            enemyBars[i] = Graphic(arena, "Enemy HP " + (i + 1), 0, 0, 0, 0, EnemyHealth);
        }
        for (int i = 0; i < shotSprites.Length; i++)
            shotSprites[i] = Entity("Shot " + (i + 1), abilityArt[1]);
        for (int i = 0; i < burstSprites.Length; i++)
            burstSprites[i] = Entity("Burst " + (i + 1), explosions[0]);

        var footer = Graphic(pane, "Combat status", .027f, 0, 1, .064f, Hull).rectTransform;
        shotsText = Label(footer, "Shot totals", .043f, .13f, .96f, .86f, 37, Secondary);

        BuildSortieFlow(attack, hull);
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
            hpText.text = $"체력 {state.Hp}/{state.Hull}";
            shieldText.text = $"쉴드 {shieldTenths / 10f:0.0}초";
            Fill(hpFill, state.Hp / (float)state.Hull);
            Fill(shieldFill, state.Shield / TabletCombat.MaxShield);
            shotsText.text = $"발사 {state.FiredShots}발   ·   대기 {state.PendingShots}발   ·   방어 {state.BlockedDamage}";
        }

        float unit = Mathf.Min(arena.rect.width, arena.rect.height);
        RenderFlow(state.Elapsed);
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
            float speed = enemy.Velocity.magnitude;
            SetActive(enemyTrails[i], visible && speed > TrailThreshold);
            if (!visible) continue;
            float shipSize = unit * (enemy.Style == 2 ? .171f : enemy.Style == 1 ? .133f : .107f);
            float hit = enemy.HitTime / TabletCombat.ImpactTime;
            Vector2 position = enemy.Position + enemy.HitDirection * (RecoilDistance * hit * hit);
            float bank = Mathf.Clamp(-enemy.Velocity.x * 58f, -24f, 24f);
            Place(warnings[i], position, shipSize * 1.4f);
            if (speed > TrailThreshold)
            {
                float strength = Mathf.InverseLerp(TrailThreshold, .88f, speed);
                enemyTrails[i].sprite = enemyArt[enemy.Style];
                enemyTrails[i].color = new Color(.56f, .84f, 1f, .12f + strength * .30f);
                enemyTrails[i].rectTransform.localRotation = Quaternion.Euler(0, 0, bank);
                Place(enemyTrails[i], enemy.Position - enemy.Velocity.normalized * (.025f + strength * .038f), shipSize);
            }
            enemySprites[i].sprite = enemyArt[enemy.Style];
            enemySprites[i].color = Color.Lerp(Color.white, ImpactColors[enemy.HitColor], hit * .85f);
            enemySprites[i].rectTransform.localRotation = Quaternion.Euler(0, 0, bank);
            Place(enemySprites[i], position, shipSize);
            enemyBars[i].color = Color.Lerp(EnemyHealth, Color.white, hit);
            Place(enemyBars[i], position + Vector2.up * .075f, shipSize * .6f);
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
            float progress = 1f - fx.Lifetime / fx.Duration;
            int frames = fx.Burst ? explosions.Length : 2;
            int frame = Mathf.Min((int)(progress * frames), frames - 1);
            burstSprites[i].sprite = explosions[frame];
            Color tint = Color.Lerp(ImpactColors[fx.Color], Color.white, fx.Burst ? .45f : .28f);
            tint.a = 1f - progress * .7f;
            burstSprites[i].color = tint;
            Place(burstSprites[i], fx.Position, unit * fx.Size * (.82f + progress * .38f));
        }

        RenderSortieEffects(state, unit);
    }

    void RenderFlow(float elapsed)
    {
        float scroll = elapsed * BackgroundScroll;
        int tile = Mathf.FloorToInt(scroll);
        float bottom = tile - scroll;
        for (int i = 0; i < backdropTiles.Length; i++)
        {
            var rect = backdropTiles[i].rectTransform;
            rect.anchoredPosition = new Vector2(0, (bottom + i) * arena.rect.height);
            // Mirror each neighboring tile so the adjoining edges match.
            if (tile != backdropCycle)
                rect.localScale = new Vector3(1, (tile + i) % 2 == 0 ? 1 : -1, 1);
        }
        backdropCycle = tile;
        starFlow.SetTime(elapsed);
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

[RequireComponent(typeof(CanvasRenderer))]
internal sealed class TabletStarFlow : MaskableGraphic
{
    const float DistantScroll = .32f;
    const float NearScroll = .86f;
    const int DistantStreakCount = 22;
    const int NearStreakCount = 14;
    static readonly Color DistantStar = new Color(.66f, .78f, .95f, .38f);
    static readonly Color NearStar = new Color(.55f, .83f, 1f, .61f);
    float elapsed;

    internal void SetTime(float seconds)
    {
        if (seconds == elapsed) return;
        elapsed = seconds;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var bounds = rectTransform.rect;
        float unit = Mathf.Min(bounds.width, bounds.height);
        for (int i = 0; i < DistantStreakCount + NearStreakCount; i++)
        {
            bool near = i >= DistantStreakCount;
            float speed = (near ? NearScroll : DistantScroll) * (1 + i % 4 * .09f);
            float y = bounds.yMin + bounds.height * Mathf.Repeat(i * .4142f + .25f - elapsed * speed, 1);
            float x = bounds.xMin + bounds.width * (.015f + .97f * Mathf.Repeat(i * .6180f + .24f, 1));
            float halfWidth = unit * (near ? .00165f : .0011f);
            float length = unit * ((near ? .036f : .014f) + i % 4 * .0025f);
            Color head = near ? NearStar : DistantStar;
            Color tail = new Color(head.r, head.g, head.b, .03f);
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x - halfWidth, y), head, Vector2.zero);
            mesh.AddVert(new Vector3(x - halfWidth, y + length), tail, Vector2.zero);
            mesh.AddVert(new Vector3(x + halfWidth, y + length), tail, Vector2.zero);
            mesh.AddVert(new Vector3(x + halfWidth, y), head, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first + 2, first + 3, first);
        }
    }
}
