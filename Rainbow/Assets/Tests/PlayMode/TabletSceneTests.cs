using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class TabletSceneTests
{
    const string Scene = "03.TabletPuzzleShooter";
    static readonly FieldInfo BoardField = typeof(Game).GetField("boardData", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo ProfileField = typeof(TabletScene).GetField("progress", BindingFlags.Instance | BindingFlags.NonPublic);
    readonly List<string> profileKeys = new List<string>();

    [TearDown]
    public void DeleteTestProfiles()
    {
        foreach (string key in profileKeys) PlayerPrefs.DeleteKey(key);
        profileKeys.Clear();
    }

    [UnityTest]
    public IEnumerator WaitsForAnExplicitSortieWithoutSpendingTheClock()
    {
        yield return OpenPreparation(NewProfileKey());
        yield return new WaitForSeconds(.85f);
        var game = Game.instance;
        Assert.That(game.CanPlay, Is.False);
        Assert.That(game.TimeSeconds, Is.EqualTo(60));
        Assert.That(game.GetComponent<TabletScene>().Combat.Elapsed, Is.Zero);
        var sortie = Primary();
        Assert.That(Tap(sortie), Is.True, lastRaycast);
        Assert.That(Tap(sortie), Is.False, "No active launch button during transition");
        Assert.That(game.CanPlay, Is.False, "The launch transition keeps input locked");
        yield return WaitUntil(() => game.CanPlay);
        Assert.That(game.TimeSeconds, Is.EqualTo(60));
    }

    [UnityTest]
    public IEnumerator GameplayClickQueuesExactShotsAndBlocksRepeatedInputDuringRefill()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        Fill(game, 1, 2);
        Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True);
        Assert.That(combat.OfferedShots + game.Score, Is.Zero);

        int total = 0;
        int[] groups = { 3, 5, 12 };
        for (int i = 0; i < groups.Length; i++)
        {
            Fill(game, i + 1, groups[i]);
            Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True);
            total += groups[i];
            Assert.That(combat.OfferedShots, Is.EqualTo(total));
            int score = game.Score;
            for (int click = 0; click < 15; click++)
                Tap(game.blocks[0].blocks[0].GetComponent<Button>());
            Assert.That(game.CanPlay, Is.False);
            Assert.That(game.Score, Is.EqualTo(score), "Queued clicks must not be accepted while the board resolves");
            yield return WaitUntil(() => game.CanPlay);
            Assert.That(combat.OfferedShots, Is.EqualTo(total), "Refill is silent for the combat queue");
            Assert.That(combat.FiredShots + combat.PendingShots, Is.EqualTo(total));
        }

        Assert.That(game.GetComponentInChildren<RectMask2D>(), Is.Not.Null);
        Assert.That(GameObject.Find("Arena").GetComponent<RectMask2D>(), Is.Not.Null);
        Assert.That(GameObject.Find("Arena").GetComponent<Image>().raycastTarget, Is.False);
    }

    [UnityTest]
    public IEnumerator GreenAppearsAtFourSecondsAndBlueClickAppliesRealShield()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        combat.Advance(4.02);
        yield return null;
        var board = (int[,])BoardField.GetValue(game);
        Assert.That(game.MaxColorCount, Is.EqualTo(4));
        bool available = false;
        for (int x = 0; x < board.GetLength(0); x++)
        for (int y = 0; y < board.GetLength(1); y++)
            available |= board[x, y] == 4 && MatchRules.FindGroup(board, x, y).Count >= 3;
        Assert.That(available, Is.True);

        combat.Advance(.9);
        Assert.That(combat.Hp, Is.EqualTo(9));
        Fill(game, 4, 3);
        Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True);
        Assert.That(combat.Hp, Is.EqualTo(10));
        yield return WaitUntil(() => game.CanPlay);

        while (combat.Elapsed < 30)
        {
            combat.ApplyMatch(4, 3);
            combat.Advance(.5);
        }
        yield return null;
        Assert.That(game.MaxColorCount, Is.EqualTo(6));
        bool blueAvailable = false;
        for (int x = 0; x < board.GetLength(0); x++)
        for (int y = 0; y < board.GetLength(1); y++)
            blueAvailable |= board[x, y] == 6 && MatchRules.FindGroup(board, x, y).Count >= 3;
        Assert.That(blueAvailable, Is.True);
        Fill(game, 6, 5);
        Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True);
        Assert.That(combat.Shield, Is.EqualTo(3.75f).Within(.001f));
        yield return WaitUntil(() => game.CanPlay);
        Fill(game, 6, 8);
        Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True);
        Assert.That(combat.Shield, Is.EqualTo(6).Within(.001f));
        Assert.That(combat.OfferedShots, Is.Zero);
    }

    [UnityTest]
    public IEnumerator TwoRetriesResetBattleAndSceneExitReleasesTheAudioVoice()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        var source = Object.FindObjectOfType<Sfx>().GetComponent<AudioSource>();
        for (int run = 0; run < 2; run++)
        {
            combat.Advance(60);
            yield return null;
            Assert.That(combat.Result, Is.EqualTo(TabletResult.Failed));
            Assert.That(game.CanPlay, Is.False);
            Assert.That(combat.PendingShots + combat.ActiveShots + combat.ActiveEffects + combat.ActiveHostiles, Is.Zero);
            Assert.That(source.clip.name, Is.EqualTo("error_006"));

            yield return new WaitForEndOfFrame();
            Assert.That(Primary().GetComponentInChildren<Text>().text, Is.EqualTo("정비하기"));
            Assert.That(Tap(Primary()), Is.True, lastRaycast);
            var retry = Primary();
            Assert.That(retry.GetComponentInChildren<Text>().text, Is.EqualTo("다시 출격"));
            Assert.That(Tap(retry), Is.True, lastRaycast);
            Assert.That(source.clip.name, Is.EqualTo("click_001"));
            yield return WaitUntil(() => game.CanPlay);
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.TimeSeconds, Is.EqualTo(60));
            Assert.That(combat.Hp, Is.EqualTo(10));
            Assert.That(combat.Shield + combat.OfferedShots, Is.Zero);
            Assert.That(game.MaxColorCount, Is.EqualTo(3));
        }
        yield return SceneManager.LoadSceneAsync("02.Game");
        Assert.That(source == null, Is.True);
        Assert.That(Object.FindObjectOfType<TabletScene>(), Is.Null);
        yield return OpenTablet();
        Assert.That(Game.instance.GetComponent<TabletScene>().Combat.Hp, Is.EqualTo(10));
        Assert.That(Object.FindObjectsOfType<AudioSource>(), Has.Length.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator AFlightPaysThroughSettlementAndHangarAndAppliesPurchasesOnTheNextSortie()
    {
        string key = NewProfileKey();
        yield return OpenPreparation(key);
        var game = Game.instance;
        var tablet = game.GetComponent<TabletScene>();
        var combat = tablet.Combat;
        Assert.That(Tap(Primary()), Is.True, lastRaycast);
        yield return WaitUntil(() => game.CanPlay);

        Fill(game, 1, 5);
        Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True);
        Assert.That(combat.OfferedShots, Is.EqualTo(5));
        combat.ApplyMatch(1, 45);
        for (int i = 0; i < 45 && (combat.AwardedFlights & 1) == 0; i++) combat.Advance(.08);
        yield return null;
        Assert.That(new TabletProgress(key).Balance, Is.EqualTo(20));
        Assert.That(GameObject.Find("Reward notice").GetComponent<Text>().text, Does.Contain("+20"));

        combat.ApplyMatch(1, 45);
        while (combat.Elapsed < 45 && combat.Result == TabletResult.Playing)
        {
            combat.ApplyMatch(4, 5);
            combat.Advance(.2);
        }
        yield return null;
        Assert.That(GameObject.Find("Boss title").GetComponent<Text>().text, Does.Contain("접근"));
        while (combat.Result == TabletResult.Playing)
        {
            combat.ApplyMatch(4, 5);
            combat.Advance(.25);
        }
        yield return WaitUntil(() => tablet.Phase == TabletPhase.Settlement);
        Assert.That(combat.Result, Is.EqualTo(TabletResult.Cleared));
        Assert.That(new TabletProgress(key).Balance, Is.EqualTo(180));
        Assert.That(GameObject.Find("Reward banner"), Is.Null);
        Assert.That(GameObject.Find("Boss defeat burst"), Is.Null);
        Assert.That(game.CanPlay, Is.False);
        Assert.That(Tap(Primary()), Is.True, lastRaycast);
        Assert.That(tablet.Phase, Is.EqualTo(TabletPhase.Hangar));
        yield return new WaitForEndOfFrame();
        var attack = GameObject.Find("Upgrade Attack").GetComponent<Button>();
        var hull = GameObject.Find("Upgrade Hull").GetComponent<Button>();
        Assert.That(Tap(attack), Is.True, lastRaycast);
        Assert.That(Tap(attack), Is.True, lastRaycast);
        Assert.That(new TabletProgress(key).Balance, Is.EqualTo(140));
        yield return null;
        Assert.That(attack.interactable, Is.False);
        Assert.That(Tap(attack), Is.True, lastRaycast);
        Assert.That(new TabletProgress(key).Balance, Is.EqualTo(140));
        yield return WaitUntil(() => hull.interactable);
        Assert.That(Tap(hull), Is.True, lastRaycast);
        Assert.That(new TabletProgress(key).Balance, Is.EqualTo(105));
        Assert.That(Tap(Primary()), Is.True, lastRaycast);
        yield return WaitUntil(() => game.CanPlay);
        Assert.That(combat.Hp, Is.EqualTo(11));
        Assert.That(combat.AwardedFlights, Is.Zero);
        Assert.That(game.TimeSeconds, Is.EqualTo(60));
        combat.ApplyMatch(1, 3);
        for (int i = 0; i < 55 && combat.HitShots == 0; i++) combat.Advance(1.0 / 60);
        Assert.That(combat.TotalDamage, Is.EqualTo(1.1f).Within(.001));

        yield return OpenPreparation(key);
        Assert.That(Game.instance.GetComponent<TabletScene>().Combat.Hull, Is.EqualTo(11));
        Assert.That(new TabletProgress(key).Balance, Is.EqualTo(105));
    }

    [Test]
    public void GeneratedAssetsImportAsCenteredSprites()
    {
        foreach (string name in new[] { "player", "enemy-scout", "enemy-wing", "enemy-elite", "arena",
                     "straight", "piercing", "spread", "heal", "homing", "shield", "blast", "ring", "carrier-bow",
                     "burst-1", "burst-2", "burst-3", "burst-4" })
        {
            Sprite sprite = TabletScene.LoadSprite(name);
            Assert.That(sprite, Is.Not.Null, name);
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(.5).Within(.001), name);
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(.5).Within(.001), name);
        }
    }

    string NewProfileKey()
    {
        string key = "Rainbow.TabletScene.PlayMode." + System.Guid.NewGuid().ToString("N");
        profileKeys.Add(key);
        return key;
    }

    static IEnumerator OpenPreparation(string key)
    {
        void Bind(Scene loaded, LoadSceneMode mode)
        {
            if (loaded.name == Scene)
                ProfileField.SetValue(Game.instance.GetComponent<TabletScene>(), new TabletProgress(key));
        }
        SceneManager.sceneLoaded += Bind;
        try
        {
            yield return SceneManager.LoadSceneAsync(Scene);
        }
        finally
        {
            SceneManager.sceneLoaded -= Bind;
        }
        yield return WaitUntil(() => GameObject.Find("Tablet Sortie") != null);
        yield return new WaitForEndOfFrame();
    }

    IEnumerator OpenTablet()
    {
        yield return OpenPreparation(NewProfileKey());
        Assert.That(Tap(Primary()), Is.True, lastRaycast);
        yield return WaitUntil(() => Game.instance != null && Game.instance.CanPlay);
    }

    static Button Primary() => GameObject.Find("Tablet Sortie").GetComponent<Button>();

    static IEnumerator WaitUntil(System.Func<bool> condition)
    {
        double until = Time.realtimeSinceStartupAsDouble + 9;
        while (!condition() && Time.realtimeSinceStartupAsDouble < until)
            yield return null;
        Assert.That(condition(), Is.True);
    }

    static void Fill(Game game, int groupColor, int size)
    {
        int first = groupColor == 1 ? 2 : 1;
        int second = groupColor == 3 ? 2 : 3;
        var board = (int[,])BoardField.GetValue(game);
        for (int y = 0; y < board.GetLength(1); y++)
        for (int x = 0; x < board.GetLength(0); x++)
        {
            int value = y * board.GetLength(0) + x < size ? groupColor : (x + y) % 2 == 0 ? first : second;
            board[x, y] = value;
            var block = game.blocks[x].blocks[y];
            block.Show(value, 0);
            block.Settle();
        }
        Canvas.ForceUpdateCanvases();
    }

    static string lastRaycast;

    static bool Tap(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect = (RectTransform)button.transform;
        var canvas = button.GetComponentInParent<Canvas>();
        var position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.TransformPoint(rect.rect.center));
        var pointer = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        lastRaycast = $"Retry raycast at {position}, hit={(hits.Count > 0 ? hits[0].gameObject.name : "(none)")}, cull={button.targetGraphic.canvasRenderer.cull}";
        if (hits.Count == 0 || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) != button.gameObject)
            return false;
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        return true;
    }
}
