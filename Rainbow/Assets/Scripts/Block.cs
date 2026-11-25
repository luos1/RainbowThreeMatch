using UnityEngine;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    [SerializeField] Button btn;
    [SerializeField] Image icon;
    [SerializeField] AnimationBlock anim;
    public Color Color => color;
    public Sprite Sprite => tabletGlyph == null ? icon.sprite : tabletGlyph.sprite;
    internal RectTransform VisualRect => icon.rectTransform;
    public int colorIndex { get; private set; }

    Color color = Color.white;
    Vector2Int position;
    Image tabletGlyph;
    Sprite[] tabletGlyphs;
    Image supplyMark;


    private void Awake()
    {
        btn = GetComponent<Button>();
        icon = transform.GetChild(0).GetComponent<Image>();
        anim = icon.GetComponent<AnimationBlock>();
        if (anim == null)
        {
            anim = icon.gameObject.AddComponent<AnimationBlock>();
        }
    }


    void onClick()
    {
        Game.instance.Check((position.x, position.y));
    }
    //Position
    public void SetPosition(int x, int y)
    {
        position = new Vector2Int(x, y);
        btn.onClick.RemoveListener(onClick);
        btn.onClick.AddListener(onClick);
    }
    //Color
    void SetColor(int index)
    {
        colorIndex = index;
        if (tabletGlyph != null)
        {
            color = TabletTileArt.Background(index);
            icon.color = color;
            tabletGlyph.sprite = tabletGlyphs[index];
            return;
        }
        color = ColorHelper.Instance.GetColor(index - 1);
        icon.sprite = ColorHelper.Instance.GetSprite(index - 1);
    }
    public void ChangeSprite(int index)
    {
        SetColor(index);
        anim.ChangeBlock();
    }
    public void Pop()
    {
        colorIndex = 0;
        anim.Pop();
    }
    public void Show(int index, int height)
    {
        SetColor(index);
        anim.Drop(height);
    }

    internal void Settle() => anim.Settle();
    internal void UseTabletDropTiming() => anim.UseTabletDropTiming();

    internal void UseTabletGlyphs(Sprite[] glyphs)
    {
        tabletGlyphs = glyphs;
        tabletGlyph = TabletTileArt.AddGlyph(icon.rectTransform);
        icon.sprite = null;
        if (TryGetComponent<Image>(out var tileBacking)) tileBacking.color = TabletTileArt.Empty;
    }

    internal void MarkTabletSupply(int color)
    {
        if (supplyMark == null)
        {
            if (color == 0) return;
            supplyMark = TabletTileArt.AddGlyph(icon.rectTransform);
            supplyMark.name = "Supply preview";
            supplyMark.rectTransform.anchorMin = new Vector2(.03f, .03f);
            supplyMark.rectTransform.anchorMax = new Vector2(.97f, .97f);
        }
        supplyMark.enabled = color != 0;
        supplyMark.color = color == 4 ? new Color(.63f, 1f, .70f, .36f) : new Color(.53f, .81f, 1f, .36f);
    }

    public bool CheckDropEnd() => !anim.IsDropping;
}
