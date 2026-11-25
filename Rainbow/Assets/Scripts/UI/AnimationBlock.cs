using UnityEngine;
using DG.Tweening;

public class AnimationBlock : MonoBehaviour
{
    const int MaxHeight = 10;
    [SerializeField] float maxDropDuration = 1f;
    [SerializeField] float heightUnit = 160f;
    [SerializeField] float upScale = 1.2f;
    [SerializeField] float upScaleDuration = .2f;

    Tween dropTween;
    public bool IsDropping => dropTween != null && dropTween.IsActive();

    public void Pop(Color color)
    {
        ResetAnimation(0f);
        gameObject.SetActive(false);
        Sfx.Instnace.Pop(transform.position, color);
    }

    public void Drop(int height)
    {
        ResetAnimation(height * heightUnit);
        gameObject.SetActive(true);
        dropTween = transform.DOLocalMoveY(0f, (float)height / MaxHeight * maxDropDuration)
            .SetAutoKill(true)
            .OnKill(() => dropTween = null);
    }

    public void ChangeBlock()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * upScale, upScaleDuration, 3);
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    void ResetAnimation(float positionY)
    {
        transform.DOKill();
        transform.localScale = Vector3.one;
        var position = transform.localPosition;
        position.y = positionY;
        transform.localPosition = position;
    }
}
