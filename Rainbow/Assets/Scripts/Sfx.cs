using UnityEngine;

public class Sfx : MonoBehaviour
{
    enum Priority { Miss, Match, RoundEnd, Retry }

    [SerializeField] GameObject boom;
    [SerializeField] AudioClip match;
    [SerializeField] AudioClip bigMatch;
    [SerializeField] AudioClip miss;
    [SerializeField] AudioClip roundEnd;
    [SerializeField] AudioClip retry;

    GameObject puff;
    AudioSource voice;
    Priority lastPriority;
    float nextRepeat;

    void Awake()
    {
        voice = GetComponent<AudioSource>();
        if (voice == null)
            voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.loop = false;
        voice.spatialBlend = 0f;
    }

    internal void PlayMatch(int tileCount, bool chained)
    {
        bool accent = chained || tileCount >= 6;
        float pitch = 1f + Mathf.Clamp((tileCount - 3) * .014f, 0f, .10f);
        PlayCue(accent ? bigMatch : match, Priority.Match, accent ? .42f : .34f, pitch, .08f);
    }

    internal void PlayMiss() => PlayCue(miss, Priority.Miss, .22f, 1f, .18f);
    internal void PlayRoundEnd() => PlayCue(roundEnd, Priority.RoundEnd, .34f, .96f, .18f);
    internal void PlayRetry() => PlayCue(retry, Priority.Retry, .30f, 1.03f, .12f);

    void PlayCue(AudioClip clip, Priority priority, float volume, float pitch, float interval)
    {
        if (clip == null || !isActiveAndEnabled)
            return;

        float now = Time.unscaledTime;
        if ((priority == lastPriority && now < nextRepeat) || (voice.isPlaying && priority < lastPriority))
            return;

        // Replace the active cue on one source so transients never sum.
        voice.Stop();
        voice.clip = clip;
        voice.volume = volume;
        voice.pitch = pitch;
        voice.Play();
        lastPriority = priority;
        nextRepeat = now + interval;
    }

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

    void OnDisable()
    {
        Clear();
        if (voice == null)
            return;
        voice.Stop();
        voice.clip = null;
        nextRepeat = 0f;
    }
}
