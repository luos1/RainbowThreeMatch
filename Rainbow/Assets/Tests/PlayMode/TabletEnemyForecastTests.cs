using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class TabletEnemyForecastTests
{
    const string SceneName = "03.TabletPuzzleShooter";
    string profileKey;

    [TearDown]
    public void DeleteTestProfile()
    {
        PlayerPrefs.DeleteKey(profileKey);
    }

    [UnityTest]
    public IEnumerator EnemyForecastShowsAttackRepairAndShieldAtTheRelevantMoments()
    {
        profileKey = "Rainbow.TabletForecast.PlayMode." + Guid.NewGuid().ToString("N");
        void BindProfile(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName) return;
            var profile = typeof(TabletScene).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic);
            profile.SetValue(Game.instance.GetComponent<TabletScene>(), new TabletProgress(profileKey));
        }
        SceneManager.sceneLoaded += BindProfile;
        try
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
        }
        finally
        {
            SceneManager.sceneLoaded -= BindProfile;
        }
        yield return null;
        var game = Game.instance;
        var tablet = game.GetComponent<TabletScene>();
        var forecast = GameObject.Find("Combat HUD").transform.Find("Enemy attack forecast")?.GetComponent<Text>();
        Assert.That(forecast, Is.Not.Null);
        Assert.That(forecast.raycastTarget, Is.False);
        Assert.That(forecast.gameObject.activeSelf, Is.False);

        GameObject.Find("Tablet Sortie").GetComponent<Button>().onClick.Invoke();
        double deadline = Time.realtimeSinceStartupAsDouble + 5;
        while (!game.CanPlay && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;
        Assert.That(game.CanPlay, Is.True);
        // Drive the in-flight clock explicitly so assertions hold at any editor frame rate.
        game.enabled = false;
        try
        {
            tablet.Tick(2.5f - tablet.Combat.Elapsed);
            Assert.That(forecast.text, Does.Contain("정찰기"));
            tablet.Tick(4.9f - tablet.Combat.Elapsed);
            Assert.That(forecast.text, Does.Contain("초록 수리"));

            while (tablet.Combat.Elapsed < 38.6f && tablet.Combat.Result == TabletResult.Playing)
            {
                if (tablet.Combat.Hp <= tablet.Combat.Hull - 3) tablet.Match(4, 3);
                tablet.Tick(Math.Min(.25f, 38.6f - tablet.Combat.Elapsed));
            }
            Assert.That(tablet.Combat.Result, Is.EqualTo(TabletResult.Playing));
            Assert.That(forecast.text, Does.Contain("포격기"));
            Assert.That(forecast.text, Does.Contain("주황 관통탄"));

            tablet.Tick(40.05f - tablet.Combat.Elapsed);
            Assert.That(forecast.text, Does.Contain("포격탄"));
            Assert.That(forecast.text, Does.Contain("파랑 쉴드"));

            int hp = tablet.Combat.Hp;
            tablet.Match(6, 3);
            Assert.That(forecast.text, Does.Contain("쉴드 전개"));
            tablet.Tick(1.5f);
            Assert.That(tablet.Combat.BlockedDamage, Is.EqualTo(1));
            Assert.That(tablet.Combat.Hp, Is.EqualTo(hp));
            Assert.That(forecast.text, Does.Not.Contain("포격탄"));

            tablet.ResetRound();
            Assert.That(forecast.gameObject.activeSelf, Is.False);
        }
        finally
        {
            game.enabled = true;
        }
    }
}
