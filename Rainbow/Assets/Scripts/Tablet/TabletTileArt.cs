using UnityEngine;
using UnityEngine.UI;

internal static class TabletTileArt
{
    internal static readonly Color Empty = new Color32(14, 21, 37, 255);
    static readonly Color[] Backgrounds =
    {
        Color.clear,
        new Color32(193, 51, 50, 255),
        new Color32(205, 104, 35, 255),
        new Color32(186, 145, 25, 255),
        new Color32(35, 128, 66, 255),
        new Color32(39, 129, 163, 255),
        new Color32(29, 59, 133, 255),
        new Color32(105, 53, 151, 255)
    };

    internal static Color Background(int color, bool available = true)
    {
        Color background = Backgrounds[color];
        background.a = available ? 1f : .38f;
        return background;
    }

    internal static Image AddGlyph(RectTransform parent)
    {
        var item = new GameObject("Effect glyph", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        item.layer = parent.gameObject.layer;
        var rect = (RectTransform)item.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(.11f, .11f);
        rect.anchorMax = new Vector2(.89f, .89f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = item.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
}
