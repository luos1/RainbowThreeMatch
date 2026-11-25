using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        AddLegalMatchAwayFromTheTestedPair(game);
        Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True);
        Assert.That(combat.OfferedShots + game.Score, Is.Zero);
        Assert.That(game.CanPlay, Is.True, "A miss with other available matches keeps the board open.");

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
        combat.Advance(Game.TabletSupplyPreviewSeconds + .03);
        yield return null;
        var board = (int[,])BoardField.GetValue(game);
        Assert.That(game.MaxColorCount, Is.EqualTo(4));
        bool available = false;
        for (int x = 0; x < board.GetLength(0); x++)
        for (int y = 0; y < board.GetLength(1); y++)
            available |= board[x, y] == 4 && MatchRules.FindGroup(board, x, y).Count >= 3;
        Assert.That(available, Is.True);

        combat.Advance(4.93 - combat.Elapsed);
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
        combat.Advance(Game.TabletSupplyPreviewSeconds + .03);
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
            yield return DeployReinforcementsAndMissAgain(game, combat);
            combat.Advance(60);
            yield return null;
            Assert.That(combat.Result, Is.EqualTo(TabletResult.Failed));
            Assert.That(game.CanPlay, Is.False);
            Assert.That(combat.PendingShots + combat.ActiveShots + combat.ActiveEffects + combat.ActiveHostiles, Is.Zero);
            Assert.That(source.clip.name, Is.EqualTo("error_006"));
            Assert.That(combat.FailureStreak + combat.ReinforcementsPending, Is.Zero);

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
            Assert.That(combat.PlannedEnemies, Is.EqualTo(TabletCombat.EnemyCount));
            Assert.That(combat.ReinforcementsArrived, Is.Zero);
        }
        yield return DeployReinforcementsAndMissAgain(game, combat);
        yield return SceneManager.LoadSceneAsync("02.Game");
        Assert.That(source == null, Is.True);
        Assert.That(Object.FindObjectOfType<TabletScene>(), Is.Null);
        yield return OpenTablet();
        var reopened = Game.instance.GetComponent<TabletScene>().Combat;
        Assert.That(reopened.Hp, Is.EqualTo(10));
        Assert.That(reopened.FailureStreak + reopened.ReinforcementsPending + reopened.ReinforcementsArrived, Is.Zero);
        Assert.That(reopened.PlannedEnemies, Is.EqualTo(TabletCombat.EnemyCount));
        Assert.That(Object.FindObjectsOfType<AudioSource>(), Has.Length.EqualTo(1));
    }

    static IEnumerator DeployReinforcementsAndMissAgain(Game game, TabletCombat combat)
    {
        AddLegalMatchAwayFromTheTestedPair(game);
        int arrived = combat.ReinforcementsArrived;
        for (int i = 0; i < 4; i++)
        {
            game.Check((0, 0));
            yield return null;
        }
        Assert.That(combat.FailureStreak, Is.EqualTo(1));
        Assert.That(combat.ReinforcementsPending + combat.ReinforcementsArrived - arrived, Is.EqualTo(5));
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

    [UnityTest]
    public IEnumerator TabletTileAndPaletteShareAnEffectGlyphWhilePortraitKeepsTheFace()
    {
        yield return OpenTablet();
        var tabletGame = Game.instance;
        var first = tabletGame.blocks[0].blocks[0];
        Assert.That(first.Sprite.name, Does.StartWith("effect-"));
        Assert.That(first.GetComponent<Image>().color, Is.EqualTo(TabletTileArt.Empty));
        var palette = GameObject.Find("Color " + first.colorIndex).transform.Find("Color icon/Effect glyph").GetComponent<Image>();
        Assert.That(palette.sprite, Is.SameAs(first.Sprite));
        foreach (string name in new[] { "straight", "piercing", "spread", "repair", "homing", "shield", "blast" })
        {
            var sprite = TabletScene.LoadSprite("effect-" + name);
            Assert.That(sprite, Is.Not.Null, name);
            Assert.That(sprite.pivot.x / sprite.rect.width, Is.EqualTo(.5).Within(.001), name);
            Assert.That(sprite.pivot.y / sprite.rect.height, Is.EqualTo(.5).Within(.001), name);
        }

        yield return SceneManager.LoadSceneAsync("02.Game");
        yield return WaitUntil(() => Game.instance != null && Game.instance.CanPlay);
        var classic = Game.instance.blocks[0].blocks[0];
        Assert.That(classic.Sprite, Is.SameAs(ColorHelper.Instance.GetSprite(classic.colorIndex - 1)));
        Assert.That(classic.Sprite.name, Does.Not.StartWith("effect-"));
    }

    [UnityTest]
    public IEnumerator UnlockDoesNotChangeThePaletteInsideAResolvingTabletMove()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        combat.Advance(3.70 - combat.Elapsed);
        Fill(game, 1, 3);
        var distant = game.blocks[4].blocks[8];
        distant.Show(distant.colorIndex, 9);
        Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True);
        combat.Advance(.35);
        yield return null;
        yield return new WaitForEndOfFrame();
        Assert.That(game.CanPlay, Is.False);
        Assert.That(game.MaxColorCount, Is.EqualTo(3), "The new color must wait until the current refill settles.");
        yield return WaitUntil(() => game.CanPlay);
        Assert.That(game.MaxColorCount, Is.EqualTo(4));
    }

    [UnityTest]
    public IEnumerator FirstRepairSupplyDoesNotSilentlyOverwriteAnIdleBoard()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        Fill(game, 1, 3);
        var board = (int[,])BoardField.GetValue(game);
        var before = (int[,])board.Clone();
        combat.Advance(4.025 - combat.Elapsed);
        yield return WaitUntil(() => game.MaxColorCount == 4);
        Assert.That(board, Is.EqualTo(before), "The reserved repair tiles must be previewed before they change.");
        Assert.That(game.CanPlay, Is.True, "The preview must leave the board interactive.");
        Assert.That(GameObject.Find("Match rule").GetComponent<Text>().text, Does.Contain("준비"));
        combat.Advance(Game.TabletSupplyPreviewSeconds + .04);
        yield return null;
        Assert.That(board[1, 4], Is.EqualTo(4));
        Assert.That(MatchRules.FindGroup(board, 1, 4).Count, Is.GreaterThanOrEqualTo(3));
    }

    [UnityTest]
    public IEnumerator ThreeLegalMissesDeployTenShipsBeforeTheOldSchedule()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        combat.Advance(1);
        var board = AddLegalMatchAwayFromTheTestedPair(game);
        Assert.That(MatchRules.HasMatch(board), Is.True);
        var block = game.blocks[0].blocks[0].GetComponent<Button>();
        for (int tap = 0; tap < 2; tap++)
        {
            Assert.That(Tap(block), Is.True, lastRaycast);
            Assert.That(GameObject.Find("Tablet miss status").GetComponent<Text>().text, Does.Contain($"{tap + 1}/3"));
            yield return null;
        }
        Assert.That(combat.Enemies.Count(enemy => enemy.Spawned), Is.EqualTo(5));
        float timeBeforeTap = combat.Elapsed;
        Assert.That(Tap(block), Is.True, lastRaycast);
        Assert.That(combat.FailureStreak, Is.Zero);
        Assert.That(combat.Elapsed, Is.EqualTo(timeBeforeTap));
        Assert.That(combat.Enemies.Count(enemy => enemy.Spawned && enemy.Hp > 0), Is.EqualTo(15));
        Assert.That(GameObject.Find("Warning message").GetComponent<Text>().text, Does.Contain("2배 투입"));
        combat.Advance(.65);
        Assert.That(combat.Elapsed, Is.LessThan(3));
        Assert.That(combat.Enemies.Count(enemy => enemy.Spawned && enemy.Position.y <= 1), Is.EqualTo(15));
        Assert.That(combat.ReinforcementsArrived, Is.EqualTo(5));
        Assert.That(game.Score, Is.Zero);
    }

    [UnityTest]
    public IEnumerator SecondMissShowsBothEntryArrowsUntilASuccessfulInput()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        AddLegalMatchAwayFromTheTestedPair(game);
        var miss = game.blocks[0].blocks[0].GetComponent<Button>();
        Assert.That(Tap(miss), Is.True, lastRaycast);
        Assert.That(GameObject.Find("Reinforcement warning"), Is.Null);
        yield return null;

        Assert.That(Tap(miss), Is.True, lastRaycast);
        var warning = GameObject.Find("Reinforcement warning");
        Assert.That(warning, Is.Not.Null);
        Assert.That(warning.transform.Find("Warning banner/Warning message").GetComponent<Text>().text, Does.Contain("위기 경보"));
        var left = warning.transform.Find("Left entry/Left arrow").GetComponent<Text>();
        var right = warning.transform.Find("Right entry/Right arrow").GetComponent<Text>();
        Assert.That(left.text, Is.EqualTo("→"));
        Assert.That(right.text, Is.EqualTo("←"));
        Assert.That(left.raycastTarget || right.raycastTarget, Is.False);
        Fill(game, 1, 3);
        Assert.That(Tap(miss), Is.True, lastRaycast);
        Assert.That(warning.activeSelf, Is.False);
        Assert.That(combat.FailureStreak, Is.Zero);
        Assert.That(combat.OfferedShots, Is.EqualTo(3));
    }

    [UnityTest]
    public IEnumerator OnlyOneMissPerFrameCountsAndSuccessResetsWhileRefillIgnoresTaps()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        AddLegalMatchAwayFromTheTestedPair(game);
        for (int i = 0; i < 10; i++) game.Check((0, 0));
        game.Check((-1, 5));
        Assert.That(combat.FailureStreak, Is.EqualTo(1));
        yield return null;
        game.Check((0, 0));
        Assert.That(combat.FailureStreak, Is.EqualTo(2));
        game.Check((0, 5));
        Assert.That(combat.FailureStreak, Is.Zero);
        Assert.That(game.CanPlay, Is.False);
        for (int i = 0; i < 10; i++) game.Check((0, 0));
        Assert.That(combat.ReinforcementsPending, Is.Zero);
        Assert.That(combat.FailureStreak, Is.Zero);
    }

    [UnityTest]
    public IEnumerator NoMatchBoardRebuildsWithoutChargingAFailure()
    {
        yield return OpenTablet();
        var game = Game.instance;
        var combat = game.GetComponent<TabletScene>().Combat;
        Fill(game, 1, 2);
        var board = (int[,])BoardField.GetValue(game);
        Assert.That(MatchRules.HasMatch(board), Is.False);
        Assert.That(Tap(game.blocks[0].blocks[0].GetComponent<Button>()), Is.True, lastRaycast);
        Assert.That(game.CanPlay, Is.False);
        Assert.That(combat.FailureStreak + combat.ReinforcementsPending, Is.Zero);
        yield return WaitUntil(() => game.CanPlay);
        Assert.That(MatchRules.HasMatch(board), Is.True);
        Assert.That(game.Score, Is.Zero);
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

    static int[,] AddLegalMatchAwayFromTheTestedPair(Game game)
    {
        Fill(game, 1, 2);
        var board = (int[,])BoardField.GetValue(game);
        for (int x = 0; x < 3; x++)
        {
            board[x, 5] = 3;
            game.blocks[x].blocks[5].Show(3, 0);
            game.blocks[x].blocks[5].Settle();
        }
        return board;
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
