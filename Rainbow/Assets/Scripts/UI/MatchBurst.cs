using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchBurst : MonoBehaviour
{
    const int MaxVisuals = 84;
    const int MaxFlashes = 8;

    [SerializeField] RectTransform board;
    [SerializeField] Sfx particles;

    readonly List<Piece> pool = new List<Piece>(MaxVisuals);
    readonly System.Random random = new System.Random(1739);
    RectTransform layer;
    Sprite ring;
    Sprite flash;
    Sprite star;
    Sprite shard;
    Tween shake;
    Vector2 restPosition;
    int used;

    internal void Play(HBlocks[] columns, List<(int x, int y)> group, (int x, int y) hit, bool chained)
    {
        if (!isActiveAndEnabled || board == null)
            return;

        if (layer == null)
            CreateLayer(columns);
        Clear();

        var hitBlock = columns[hit.x].blocks[hit.y];
        Vector2 impact = Center(hitBlock);
        float unit = Mathf.Min(hitBlock.VisualRect.rect.width, hitBlock.VisualRect.rect.height);
        Color color = hitBlock.Color;
        Color rim = Color.Lerp(color, new Color(.12f, .09f, .23f), .40f);
        Color light = Color.Lerp(color, Color.white, .42f);
        float power = Mathf.Clamp01((group.Count - 3) * .075f + (chained ? .32f : 0f));
        if (particles != null)
            particles.Pop(layer.TransformPoint(impact), color);

        Vector2 average = Vector2.zero;
        foreach (var cell in group)
        {
            var block = columns[cell.x].blocks[cell.y];
            var origin = Center(block);
            average += origin;
            Emit(block.Sprite, Color.white, origin, block.VisualRect.rect.size, .19f, 1.21f,
                travel: (origin - impact).normalized * unit * .09f);
        }
        average /= group.Count;

        int flashCount = Mathf.Min(group.Count, MaxFlashes);
        for (int i = 0; i < flashCount; i++)
        {
            var cell = group[i * group.Count / flashCount];
            var origin = Center(columns[cell.x].blocks[cell.y]);
            Emit(flash, WithAlpha(light, .76f), origin, Vector2.one * unit * .96f, .15f, 1.32f);
        }

        Vector2 center = Vector2.Lerp(impact, average, .4f);
        Emit(ring, WithAlpha(rim, .94f), center, Vector2.one * unit * 1.29f, .34f, 3.1f + power * .55f);
        Emit(ring, WithAlpha(color, .76f), center, Vector2.one * unit, .37f, 3.9f + power * .68f, delay: .04f);
        Emit(star, WithAlpha(light, .94f), impact, Vector2.one * unit * .84f, .2f, 1.4f, rotation: 15f);

        int scatterCount = Mathf.RoundToInt(Mathf.Lerp(14, 28, power));
        float phase = Between(0, Mathf.PI * 2);
        for (int i = 0; i < scatterCount; i++)
        {
            float angle = phase + (i + Between(-.38f, .38f)) * Mathf.PI * 2 / scatterCount;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var cell = group[i % group.Count];
            var origin = Vector2.Lerp(center, Center(columns[cell.x].blocks[cell.y]), Between(.22f, .78f));
            bool sparkle = i % 3 == 0;
            float size = unit * Between(.16f, .32f);
            Emit(sparkle ? star : shard, WithAlpha(sparkle ? Color.Lerp(color, Color.white, .23f) : rim, Between(.81f, 1f)), origin,
                new Vector2(size, size * (sparkle ? 1f : Between(.45f, .78f))), Between(.24f, .4f) + power * .06f,
                Between(.12f, .43f), travel: direction * unit * Between(.92f, 1.53f + power * .58f),
                rotation: angle * Mathf.Rad2Deg, spin: Between(-155f, 155f), delay: Between(0, .034f));
        }

        restPosition = board.anchoredPosition;
        var push = new Vector2(impact.x < 0 ? 1f : -1f, .55f) * Mathf.Lerp(4.3f, 8.2f, power);
        shake = DOTween.Punch(() => board.anchoredPosition, value => board.anchoredPosition = value,
            push, .18f, 4, .35f).OnKill(RestoreBoard);
    }

    internal void Clear()
    {
        shake?.Kill();
        for (int i = 0; i < used; i++)
            pool[i].View.gameObject.SetActive(false);
        used = 0;
        if (particles != null)
            particles.Clear();
    }

    void RestoreBoard()
    {
        shake = null;
        if (board != null)
            board.anchoredPosition = restPosition;
    }

    internal void ResetRound()
    {
        Clear();
        DropLayer();
    }

    void Update()
    {
        bool hasLivePieces = false;
        for (int i = 0; i < used; i++)
            hasLivePieces |= pool[i].Advance(Time.deltaTime);
        if (!hasLivePieces)
            used = 0;
    }

    void OnDisable() => Clear();

    void OnDestroy()
    {
        Release(ring);
        Release(flash);
        Release(star);
        Release(shard);
        DropLayer();
    }

    void DropLayer()
    {
        if (layer != null)
            Destroy(layer.gameObject);
        layer = null;
        pool.Clear();
    }

    Vector2 Center(Block block) => layer.InverseTransformPoint(block.VisualRect.TransformPoint(block.VisualRect.rect.center));

    void CreateLayer(HBlocks[] columns)
    {
        Vector2 low = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 high = new Vector2(float.MinValue, float.MinValue);
        foreach (var column in columns)
        foreach (var block in column.blocks)
        {
            var rect = (RectTransform)block.transform;
            low = Vector2.Min(low, board.InverseTransformPoint(rect.TransformPoint(rect.rect.min)));
            high = Vector2.Max(high, board.InverseTransformPoint(rect.TransformPoint(rect.rect.max)));
        }

        var root = new GameObject("Match Burst", typeof(RectTransform), typeof(RectMask2D));
        root.layer = board.gameObject.layer;
        layer = (RectTransform)root.transform;
        layer.SetParent(board, false);
        layer.anchorMin = layer.anchorMax = board.pivot;
        layer.sizeDelta = high - low + Vector2.one * 12f;
        layer.anchoredPosition = (low + high) * .5f;

        if (ring != null)
            return;
        ring = CreateShape("Match ring", (x, y) => .115f - Mathf.Abs(Mathf.Sqrt(x * x + y * y) - .78f));
        flash = CreateShape("Match flash", (x, y) => (.84f - Mathf.Sqrt(x * x + y * y)) * .09f);
        star = CreateShape("Match sparkle", (x, y) => 1.03f - Mathf.Pow(x, .65f) - Mathf.Pow(y, .65f));
        shard = CreateShape("Match shard", (x, y) => .92f - x - y * .8f);
    }

    void Emit(Sprite sprite, Color tint, Vector2 origin, Vector2 size, float duration, float scale,
        Vector2 travel = default, float rotation = 0f, float spin = 0f, float delay = 0f)
    {
        if (used == MaxVisuals)
            return;
        if (used == pool.Count)
        {
            var view = new GameObject("Burst Graphic", typeof(RectTransform), typeof(Image));
            view.layer = layer.gameObject.layer;
            view.transform.SetParent(layer, false);
            var image = view.GetComponent<Image>();
            image.raycastTarget = false;
            pool.Add(new Piece { View = image });
        }

        var piece = pool[used++];
        piece.View.sprite = sprite;
        piece.View.rectTransform.sizeDelta = size;
        piece.Tint = tint;
        piece.Origin = origin;
        piece.Travel = travel;
        piece.Duration = duration;
        piece.Scale = scale;
        piece.Rotation = rotation;
        piece.Spin = spin;
        piece.Age = -delay;
        piece.Advance(0f);
    }

    static Sprite CreateShape(string name, System.Func<float, float, float> distance)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = Mathf.Abs((x + .5f) * (2f / size) - 1f);
            float v = Mathf.Abs((y + .5f) * (2f / size) - 1f);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(distance(u, v) * 22f) * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.name = name;
        return sprite;
    }

    static void Release(Sprite sprite)
    {
        if (sprite == null)
            return;
        Destroy(sprite.texture);
        Destroy(sprite);
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    float Between(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

    sealed class Piece
    {
        internal Image View;
        internal Color Tint;
        internal Vector2 Origin, Travel;
        internal float Age, Duration, Scale, Rotation, Spin;

        internal bool Advance(float step)
        {
            Age += step;
            bool visible = Age >= 0f && Age < Duration;
            if (View.gameObject.activeSelf != visible)
                View.gameObject.SetActive(visible);
            if (!visible)
                return Age < Duration;

            float t = Age / Duration;
            float movement = t * (2f - t);
            var rect = View.rectTransform;
            rect.anchoredPosition = Origin + Travel * movement;
            rect.localRotation = Quaternion.Euler(0f, 0f, Rotation + Spin * movement);
            rect.localScale = Vector3.one * Mathf.Lerp(1f, Scale, movement);
            View.color = WithAlpha(Tint, Tint.a * (1f - t) * (1f - .3f * t));
            return true;
        }
    }
}
