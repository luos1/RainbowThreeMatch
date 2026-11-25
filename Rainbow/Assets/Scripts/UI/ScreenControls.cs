using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

internal static class ScreenControls
{
    internal static Text Label(Transform parent, string title, float y, int fontSize)
    {
        var label = Add<Text>(parent, title, y, new Vector2(760, 120));
        label.text = title;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        var outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.07f, 0.10f, 0.15f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        return label;
    }

    internal static Button GoogleButton(Transform parent, UnityAction onClick)
    {
        string asset = Application.platform == RuntimePlatform.IPhonePlayer ? "GoogleSignInIOS" : "GoogleSignInAndroid";
        var texture = Resources.Load<Texture2D>(asset);
        var size = new Vector2(530, 530f * texture.height / texture.width);
        var art = Add<RawImage>(parent, "Sign in with Google", -20, size);
        art.texture = texture;
        var button = art.gameObject.AddComponent<Button>();
        button.targetGraphic = art;
        button.onClick.AddListener(onClick);
        return button;
    }

    internal static Button Button(Transform parent, string title, float y, UnityAction onClick)
    {
        var background = Add<Image>(parent, title, y, new Vector2(530, 120));
        background.color = new Color(0.15f, 0.38f, 0.63f, 1f);
        var button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(onClick);
        Label(button.transform, title, 0, 45);
        return button;
    }

    private static T Add<T>(Transform parent, string name, float y, Vector2 size) where T : Graphic
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)item.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = size;
        return item.AddComponent<T>();
    }
}
