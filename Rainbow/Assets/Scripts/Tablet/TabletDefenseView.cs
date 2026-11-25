using UnityEngine;
using UnityEngine.UI;

internal sealed partial class TabletView
{
    static readonly Color HostileColor = new Color32(255, 209, 110, 255);
    static readonly Color BossShotColor = new Color32(255, 112, 140, 255);
    readonly Image[] hostileSprites = new Image[TabletCombat.HostileCapacity];
    readonly Image[] enemyMuzzles = new Image[TabletCombat.EnemyCount];
    readonly Image[] carrierMuzzles = new Image[3];
    TabletShieldRim shieldRim;
    Image contactSpark;

    void BuildDefenseVisuals()
    {
        shieldRim = Rect(arena, "Carrier shield rim", 0, 0, 1, 1).gameObject.AddComponent<TabletShieldRim>();
        shieldRim.raycastTarget = false;
        for (int i = 0; i < enemyMuzzles.Length; i++)
            enemyMuzzles[i] = Entity("Enemy muzzle " + (i + 1), explosions[0]);
        for (int i = 0; i < carrierMuzzles.Length; i++)
            carrierMuzzles[i] = Entity("Carrier muzzle " + (i + 1), explosions[0]);
        for (int i = 0; i < hostileSprites.Length; i++)
            hostileSprites[i] = Entity("Enemy shot " + (i + 1), abilityArt[2]);
        contactSpark = Entity("Carrier contact", explosions[0]);
    }

    void RenderCarrier(TabletCombat state)
    {
        var transform = player.rectTransform;
        var center = state.Player + Vector2.up * TabletCombat.CarrierSpriteCenter;
        var extent = new Vector2(TabletCombat.CarrierSpriteWidth, TabletCombat.CarrierSpriteHeight) * .5f;
        if (transform.anchorMin != center - extent)
        {
            transform.anchorMin = center - extent;
            transform.anchorMax = center + extent;
            transform.offsetMin = transform.offsetMax = Vector2.zero;
        }
        float hit = state.HullFlash / TabletCombat.ContactFlashTime;
        player.color = Color.Lerp(Color.white, new Color(1f, .40f, .32f), hit * .65f);
    }

    void RenderDefenseVisuals(TabletCombat state, float unit)
    {
        bool shield = state.Shield > 0;
        if (shieldRim.gameObject.activeSelf != shield)
            shieldRim.gameObject.SetActive(shield);
        if (shield)
            shieldRim.Sync(state.Player.x, state.ShieldFlash / TabletCombat.ContactFlashTime);

        for (int i = 0; i < hostileSprites.Length; i++)
        {
            var shot = state.Hostiles[i];
            SetActive(hostileSprites[i], shot.Active);
            if (!shot.Active) continue;
            var image = hostileSprites[i];
            image.color = shot.Boss ? BossShotColor : HostileColor;
            Place(image, shot.Position, unit * (shot.Boss ? .066f : .055f));
            Direction(image, shot.Direction);
        }

        for (int i = 0; i < enemyMuzzles.Length; i++)
        {
            var enemy = state.Enemies[i];
            bool flash = enemy.MuzzleTime > 0 && enemy.Hp > 0;
            SetActive(enemyMuzzles[i], flash);
            if (!flash) continue;
            Place(enemyMuzzles[i], TabletCombat.EnemyMuzzle(enemy), unit * (enemy.Style == 2 ? .068f : .043f));
        }
        for (int i = 0; i < carrierMuzzles.Length; i++)
        {
            float flash = state.MuzzleTimes[i];
            SetActive(carrierMuzzles[i], flash > 0);
            if (flash <= 0) continue;
            var tint = ImpactColors[state.LastFiredColor];
            tint.a = Mathf.Clamp01(flash / .09f);
            carrierMuzzles[i].color = tint;
            Place(carrierMuzzles[i], state.CarrierMuzzle(i), unit * .056f);
        }

        bool blocked = state.ShieldFlash >= state.HullFlash;
        float contact = Mathf.Max(state.ShieldFlash, state.HullFlash) / TabletCombat.ContactFlashTime;
        SetActive(contactSpark, contact > 0);
        if (contact <= 0) return;
        contactSpark.color = blocked ? new Color(.46f, .91f, 1f, contact) : new Color(1f, .40f, .28f, contact);
        contactSpark.sprite = blocked ? ring : explosions[0];
        Place(contactSpark, state.ContactPosition, unit * (.075f + .075f * (1 - contact)));
    }
}

[RequireComponent(typeof(CanvasRenderer))]
internal sealed class TabletShieldRim : MaskableGraphic
{
    static readonly Vector2[] Outline =
    {
        new Vector2(-TabletCombat.ShieldBaseHalfWidth, 0),
        new Vector2(-TabletCombat.ShieldShoulderHalfWidth, TabletCombat.ShieldShoulder),
        new Vector2(-TabletCombat.ShieldTopHalfWidth, TabletCombat.ShieldTop),
        new Vector2(TabletCombat.ShieldTopHalfWidth, TabletCombat.ShieldTop),
        new Vector2(TabletCombat.ShieldShoulderHalfWidth, TabletCombat.ShieldShoulder),
        new Vector2(TabletCombat.ShieldBaseHalfWidth, 0)
    };

    float center, flash;

    internal void Sync(float bowX, float hit)
    {
        if (center == bowX && flash == hit) return;
        center = bowX;
        flash = hit;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect bounds = rectTransform.rect;
        Vector2 scale = bounds.size;
        Vector2 origin = bounds.min + Vector2.right * (center * scale.x);
        for (int i = 0; i + 1 < Outline.Length; i++)
        {
            var from = origin + Vector2.Scale(Outline[i], scale);
            var to = origin + Vector2.Scale(Outline[i + 1], scale);
            var direction = (to - from).normalized;
            var across = new Vector2(-direction.y, direction.x);
            float line = Mathf.Min(scale.x, scale.y) * (.0024f + .0012f * flash);
            AddStrip(mesh, from, to, across * (line * 2.4f), new Color(.22f, .74f, 1f, .18f + .28f * flash));
            AddStrip(mesh, from, to, across * line, new Color(.51f, .90f, 1f, .78f + .22f * flash));
        }
    }

    static void AddStrip(VertexHelper mesh, Vector2 from, Vector2 to, Vector2 width, Color tint)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(from - width, tint, Vector2.zero);
        mesh.AddVert(from + width, tint, Vector2.zero);
        mesh.AddVert(to + width, tint, Vector2.zero);
        mesh.AddVert(to - width, tint, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start + 2, start + 3, start);
    }
}
