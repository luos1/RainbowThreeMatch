using UnityEngine;
using UnityEngine.UI;

public class Block : MonoBehaviour
{
    [SerializeField] Button btn;
    [SerializeField] Image icon;
    [SerializeField] AnimationBlock anim;
    public Color Color => color;
    public Sprite Sprite => icon.sprite;
    internal RectTransform VisualRect => icon.rectTransform;
    public int colorIndex { get; private set; }

    Color color = Color.white;
    Vector2Int position;


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

    public bool CheckDropEnd() => !anim.IsDropping;
}
