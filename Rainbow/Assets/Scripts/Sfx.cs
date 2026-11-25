using UnityEngine;

public class Sfx : MonoBehaviour
{
    [SerializeField] GameObject boom;

    GameObject puff;

    public void Pop(Vector3 uiPosition, Color color)
    {
        Clear();
        if (boom == null)
            return;

        puff = Instantiate(boom, uiPosition, boom.transform.rotation, transform);
        var system = puff.GetComponentInChildren<ParticleSystem>();
        if (system != null)
        {
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var settings = system.main;
            settings.startColor = color;
            system.Play(true);
        }
        Destroy(puff, .7f);
    }

    internal void Clear()
    {
        if (puff == null)
            return;
        puff.SetActive(false);
        Destroy(puff);
        puff = null;
    }

    void OnDisable() => Clear();
}
