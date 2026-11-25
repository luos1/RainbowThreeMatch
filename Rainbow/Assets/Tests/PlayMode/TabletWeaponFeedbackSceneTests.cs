using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class TabletWeaponFeedbackSceneTests
{
    const string SceneName = "03.TabletPuzzleShooter";
    static readonly FieldInfo ProgressField = typeof(TabletScene).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic);
    string profileKey;

    [TearDown]
    public void RemoveTestProfile() => PlayerPrefs.DeleteKey(profileKey);

    [UnityTest]
    public IEnumerator HangarPurchaseChangesCannonsShotsAndKillsOnNextSortie()
    {
        yield return OpenTablet();
        var tablet = Game.instance.GetComponent<TabletScene>();
        var arena = GameObject.Find("Arena").transform;
        var safeArea = GameObject.Find("Tablet Safe Area");
        var collar = arena.Find("Carrier cannon collar 1").GetComponent<Image>();
        var brace = arena.Find("Carrier armor brace 1").GetComponent<Image>();
        Assert.That(collar.gameObject.activeSelf, Is.False);
        Assert.That(brace.gameObject.activeSelf, Is.False);
        Assert.That(safeArea.GetComponent<AudioSource>(), Is.Null);

        ButtonNamed("Tablet Sortie").onClick.Invoke();
        yield return Until(() => Game.instance.CanPlay);
        FireFiveShots(tablet, arena, expectShockwave: false);
        var voice = safeArea.GetComponent<AudioSource>();
        Assert.That(voice.clip.name, Is.EqualTo("laserSmall_000"));
        float originalPitch = voice.pitch;

        tablet.Combat.Advance(TabletCombat.Duration);
        yield return Until(() => tablet.Phase == TabletPhase.Settlement);
        Assert.That(voice.isPlaying, Is.False);
        ButtonNamed("Tablet Sortie").onClick.Invoke();
        Assert.That(tablet.Phase, Is.EqualTo(TabletPhase.Hangar));
        ButtonNamed("Upgrade Attack").onClick.Invoke();
        yield return Until(() => ButtonNamed("Upgrade Hull").interactable);
        ButtonNamed("Upgrade Hull").onClick.Invoke();
        Assert.That(collar.gameObject.activeSelf, Is.False, "The settled sortie keeps its equipment.");

        ButtonNamed("Tablet Sortie").onClick.Invoke();
        yield return Until(() => Game.instance.CanPlay);
        Assert.That(tablet.Combat.Hp, Is.EqualTo(TabletCombat.MaxHp + 1));
        for (int i = 0; i < 3; i++)
        {
            var upgraded = arena.Find("Carrier cannon collar " + (i + 1)).GetComponent<RectTransform>();
            Assert.That(upgraded.gameObject.activeSelf, Is.True);
            Assert.That(upgraded.anchorMin, Is.EqualTo(tablet.Combat.CarrierMuzzle(i)));
            Assert.That(upgraded.sizeDelta.x, Is.Positive);
        }
        Assert.That(brace.gameObject.activeSelf, Is.True);
        Assert.That(Shockwave(arena), Is.Null);
        FireFiveShots(tablet, arena, expectShockwave: true);
        Assert.That(safeArea.GetComponents<AudioSource>(), Has.Length.EqualTo(1));
        Assert.That(voice.clip.name, Is.EqualTo("laserSmall_003"));
        Assert.That(voice.pitch, Is.LessThan(originalPitch));
        tablet.Tick(1.5f);
        Assert.That(Shockwave(arena), Is.Null, "Defeat feedback expires with the burst.");

        yield return SceneManager.LoadSceneAsync("02.Game");
        Assert.That(voice == null, Is.True);
        Assert.That(GameObject.Find("Tablet Safe Area"), Is.Null);
        Assert.That(UnityEngine.Object.FindObjectsOfType<AudioSource>(), Has.Length.EqualTo(1));
    }

    IEnumerator OpenTablet()
    {
        profileKey = "Rainbow.TabletWeaponFeedback.PlayMode." + Guid.NewGuid().ToString("N");
        var profile = new TabletProgress(profileKey);
        profile.Credit(profile.BeginRun(), TabletProgress.BossBit);

        void Bind(Scene loaded, LoadSceneMode mode)
        {
            if (loaded.name == SceneName)
                ProgressField.SetValue(Game.instance.GetComponent<TabletScene>(), profile);
        }
        SceneManager.sceneLoaded += Bind;
        try
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
        }
        finally
        {
            SceneManager.sceneLoaded -= Bind;
        }
        yield return Until(() => GameObject.Find("Tablet Sortie") != null);
    }

    static void FireFiveShots(TabletScene tablet, Transform arena, bool expectShockwave)
    {
        var combat = tablet.Combat;
        Assert.That(combat.FiredShots, Is.Zero);
        tablet.Match(1, 5);
        Assert.That(combat.OfferedShots, Is.EqualTo(5));
        for (int i = 0; i < 75 && combat.HitShots == 0; i++) tablet.Tick(1f / 60);
        Assert.That(combat.HitShots, Is.Positive);
        Assert.That(combat.EnemiesDefeated, Is.Zero);
        Assert.That(Shockwave(arena), Is.Null, "A nonlethal impact has no defeat shockwave.");

        for (int i = 0; i < 75 && combat.EnemiesDefeated == 0; i++) tablet.Tick(1f / 60);
        Assert.That(combat.EnemiesDefeated, Is.Positive);
        var shockwave = Shockwave(arena);
        if (expectShockwave)
        {
            Assert.That(shockwave, Is.Not.Null);
            Assert.That(shockwave.sprite.name, Is.EqualTo("ring"));
            Assert.That(shockwave.rectTransform.sizeDelta.x, Is.Positive);
            Assert.That(shockwave.color.a, Is.Positive);
        }
        else
            Assert.That(shockwave, Is.Null);

        for (int i = 0; i < 75 && combat.PendingShots > 0; i++) tablet.Tick(1f / 60);
        Assert.That(combat.FiredShots, Is.EqualTo(5));
        Assert.That(combat.PendingShots, Is.Zero);
    }

    static Image Shockwave(Transform arena) => arena.GetComponentsInChildren<Image>()
        .FirstOrDefault(image => image.name.StartsWith("Defeat shockwave ", StringComparison.Ordinal));

    static Button ButtonNamed(string name) => GameObject.Find(name).GetComponent<Button>();

    static IEnumerator Until(Func<bool> condition)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 8;
        while (!condition() && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;
        Assert.That(condition(), Is.True);
    }
}
