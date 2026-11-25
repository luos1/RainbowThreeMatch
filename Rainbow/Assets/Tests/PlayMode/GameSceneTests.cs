using System;
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
using Random = UnityEngine.Random;

public class GameSceneTests
{
    [UnityTest]
    public IEnumerator InitialBoardAndHudFitWithinGameViewport()
    {
        yield return SceneManager.LoadSceneAsync("02.Game");
        var game = Game.instance;
        Assert.That(game, Is.Not.Null);
        yield return WaitFor(() => game.CanPlay, "initial drop");
        Canvas.ForceUpdateCanvases();

        var canvas = game.GetComponentInParent<Canvas>();
        Assert.That(canvas, Is.Not.Null);
        var corners = new Vector3[4];
        foreach (var column in game.blocks)
        foreach (var block in column.blocks)
        {
            AssertInsideViewport((RectTransform)block.transform, canvas, corners);
        }

        AssertInsideViewport((RectTransform)Object.FindObjectOfType<ScoreText>().transform, canvas, corners);
        AssertInsideViewport((RectTransform)Object.FindObjectOfType<GradeText>().transform, canvas, corners);
        AssertInsideViewport((RectTransform)Object.FindObjectOfType<TimerText>().transform, canvas, corners);
    }

    [UnityTest]
    public IEnumerator MatchRefillsAndRetryRestartsTheTimedRound()
    {
        var randomState = Random.state;
        float timeScale = Time.timeScale;
        try
        {
            Random.InitState(1574);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("02.Game");
            var game = Game.instance;
            Assert.That(game, Is.Not.Null);
            yield return WaitFor(() => game.CanPlay, "initial drop");
            Assert.That(game.TimeSeconds, Is.EqualTo(60));
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.Grade, Is.Zero);

            int[,] board = ReadBoard(game);
            var match = FindMatch(board);
            var button = game.blocks[match.x].blocks[match.y].GetComponent<Button>();
            ClickVisibleButton(button);
            Assert.That(game.CanPlay, Is.False);
            Assert.That(button.GetComponent<Block>().colorIndex, Is.Zero);

            int expectedScore = MatchRules.GetScore(match.count);
            button.onClick.Invoke();
            Assert.That(game.Score, Is.EqualTo(expectedScore));
            yield return WaitFor(() => game.CanPlay, "refill");
            FindMatch(ReadBoard(game));

            Time.timeScale = 12f;
            yield return WaitFor(() => game.TimeSeconds == 0, "round timeout", 25f);
            Assert.That(game.CanPlay, Is.False);
            button.onClick.Invoke();
            Assert.That(game.Score, Is.EqualTo(expectedScore));

            var retry = Array.Find(Object.FindObjectsOfType<Button>(true), candidate =>
                candidate.onClick.GetPersistentEventCount() > 0 &&
                candidate.onClick.GetPersistentMethodName(0) == nameof(Game.OnClickRetry));
            Assert.That(retry, Is.Not.Null);
            Assert.That(retry.gameObject.activeInHierarchy, Is.True);

            Time.timeScale = 1f;
            yield return null;
            ClickVisibleButton(retry);
            yield return WaitFor(() => game.CanPlay, "retry drop");
            Assert.That(retry.gameObject.activeInHierarchy, Is.False);
            Assert.That(game.Score, Is.Zero);
            Assert.That(game.Grade, Is.Zero);
            Assert.That(game.TimeSeconds, Is.EqualTo(60));
            FindMatch(ReadBoard(game));
        }
        finally
        {
            Time.timeScale = timeScale;
            Random.state = randomState;
        }
    }

    [UnityTest]
    public IEnumerator BurstIsBoundedAndClearsWhenRoundEnds()
    {
        yield return SceneManager.LoadSceneAsync("02.Game");
        var game = Game.instance;
        yield return WaitFor(() => game.CanPlay, "initial drop");
        var board = (RectTransform)game.transform.Find("Game");
        var restingPosition = board.anchoredPosition;
        var match = FindMatch(ReadBoard(game));
        ClickVisibleButton(game.blocks[match.x].blocks[match.y].GetComponent<Button>());

        var burst = board.Find("Match Burst");
        Assert.That(burst, Is.Not.Null, "a match creates a board-only visual layer");
        var graphics = burst.GetComponentsInChildren<Graphic>();
        Assert.That(graphics.Length, Is.InRange(match.count + 5, 84));
        Assert.That(Array.TrueForAll(graphics, graphic => !graphic.raycastTarget), Is.True);
        Assert.That(game.Score, Is.EqualTo(MatchRules.GetScore(match.count)));

        yield return new WaitForSecondsRealtime(.7f);
        Assert.That(burst.GetComponentsInChildren<Graphic>(), Is.Empty);
        Assert.That(board.anchoredPosition, Is.EqualTo(restingPosition));
        yield return WaitFor(() => game.CanPlay, "refill before timeout");
        match = FindMatch(ReadBoard(game));
        ClickVisibleButton(game.blocks[match.x].blocks[match.y].GetComponent<Button>());
        Assert.That(burst.GetComponentsInChildren<Graphic>(), Is.Not.Empty);
        var particles = Object.FindObjectOfType<Sfx>().transform;
        Assert.That(particles.GetComponentsInChildren<ParticleSystem>().Length, Is.LessThanOrEqualTo(1));

        typeof(Game).GetField("remainingSeconds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 0f);
        yield return WaitFor(() => game.TimeSeconds == 0, "timeout during burst");
        Assert.That(burst.GetComponentsInChildren<Graphic>(), Is.Empty);
        Assert.That(particles.GetComponentsInChildren<ParticleSystem>(), Is.Empty);
        Assert.That(board.anchoredPosition, Is.EqualTo(restingPosition));

        game.OnClickRetry();
        yield return WaitFor(() => game.CanPlay, "retry");
        Assert.That(board.Find("Match Burst"), Is.Null);
        Assert.That(game.Score, Is.Zero);
        Assert.That(game.TimeSeconds, Is.EqualTo(60));
        Assert.That(board.anchoredPosition, Is.EqualTo(restingPosition));
    }

    [UnityTest]
    public IEnumerator LargerAndChainedBurstsAddLayersWithoutLeavingSceneObjects()
    {
        yield return SceneManager.LoadSceneAsync("02.Game");
        var game = Game.instance;
        yield return WaitFor(() => game.CanPlay, "initial drop");
        var effect = game.GetComponent<MatchBurst>();
        var board = game.transform.Find("Game");
        var small = new List<(int, int)> { (2, 4), (2, 3), (2, 5) };
        effect.Play(game.blocks, small, (2, 4), false);
        var burst = board.Find("Match Burst");
        int regular = burst.childCount;
        effect.Play(game.blocks, small, (2, 4), true);
        int chained = burst.childCount;
        Assert.That(chained, Is.GreaterThan(regular));

        var fullBoard = new List<(int, int)>();
        for (int x = 0; x < game.blocks.Length; x++)
        for (int y = 0; y < game.blocks[x].blocks.Length; y++)
            fullBoard.Add((x, y));
        effect.Play(game.blocks, fullBoard, (2, 4), true);
        Assert.That(burst.childCount, Is.InRange(chained + 1, 84));
        Assert.That(Array.Exists(burst.GetComponentsInChildren<Image>(), image => image.sprite.name == "Match ring"), Is.True);
        Assert.That(game.CanPlay, Is.True, "presentation does not block input");
        Assert.That(game.Score, Is.Zero, "presentation does not score the board");

        yield return SceneManager.LoadSceneAsync("00.Loading");
        Assert.That(burst == null, Is.True, "the scene owns all effect objects");
    }

    [UnityTest]
    public IEnumerator PortraitFeedbackShowsRealMatchAndFailure()
    {
        yield return SceneManager.LoadSceneAsync("02.Game");
        var game = Game.instance;
        yield return WaitFor(() => game.CanPlay, "portrait initial drop");
        var panel = (RectTransform)game.transform.Find("Match Feedback");
        Assert.That(panel, Is.Not.Null, "the game shows a passive color-cycle and match feedback panel");
        Assert.That(panel.GetComponentsInChildren<Button>(), Is.Empty);
        Assert.That(Array.TrueForAll(panel.GetComponentsInChildren<Graphic>(), graphic => !graphic.raycastTarget), Is.True);
        AssertInsideViewport(panel, game.GetComponentInParent<Canvas>(), new Vector3[4]);

        var palette = panel.Find("Palette wrap").GetComponent<Text>();
        var status = panel.Find("Match status").GetComponent<Text>();
        var detail = panel.Find("Match detail").GetComponent<Text>();
        Assert.That(palette.text, Is.EqualTo("3/7색 · 노랑 → 빨강"));
        Assert.That(palette.font.HasCharacter('빨'), Is.True);
        Assert.That(palette.font.HasCharacter('→'), Is.True);

        var field = typeof(Game).GetField("boardData", BindingFlags.Instance | BindingFlags.NonPublic);
        var board = (int[,])field.GetValue(game);
        for (int x = 0; x < board.GetLength(0); x++)
        for (int y = 0; y < board.GetLength(1); y++)
        {
            board[x, y] = (x + 2 * y) % 3 + 1;
            game.blocks[x].blocks[y].Show(board[x, y], 0);
        }
        yield return null;
        var edge = game.blocks[0].blocks[0].GetComponent<Button>();
        var edgeRect = (RectTransform)edge.transform;
        ClickVisibleButton(edge, new Vector2(edgeRect.rect.xMin - 4f, edgeRect.rect.center.y));
        Assert.That(status.text, Is.EqualTo("빨강 1개만 연결됐어요"));
        Assert.That(detail.text, Does.Contain("3개 이상"));
        Assert.That(game.Score, Is.Zero);
        Assert.That(game.CanPlay, Is.True);

        foreach (var point in new[] { (2, 3), (2, 4), (2, 5), (3, 4), (1, 4) })
        {
            board[point.Item1, point.Item2] = 1;
            game.blocks[point.Item1].blocks[point.Item2].Show(1, 0);
        }
        yield return null;
        int count = MatchRules.FindGroup(board, 2, 4).Count;
        typeof(Game).GetProperty(nameof(Game.Score)).SetValue(game, 995);
        ClickVisibleButton(game.blocks[2].blocks[4].GetComponent<Button>());
        Assert.That(status.text, Is.EqualTo($"빨강 {count}개 매치!"));
        yield return WaitFor(() => detail.text.Contains("→"), "neighbor transitions");
        Assert.That(detail.text, Does.Contain("주황 → 노랑"));
        Assert.That(detail.text, Does.Contain("노랑 → 빨강"));
        Assert.That(palette.text, Is.EqualTo("4/7색 · 초록 → 빨강"));
        Assert.That(game.Score, Is.EqualTo(995 + MatchRules.GetScore(count)));
        Assert.That(panel.Find("Color 2/Previous color").GetComponent<Image>().enabled, Is.True);
        Assert.That(panel.Find("Color 1/Next color").GetComponent<Image>().enabled, Is.True);
    }

    [TestCase(1080, 1920, 0, 0, 0)]
    [TestCase(1080, 1917, 0, 0, 0)]
    [TestCase(1080, 2337, 24, 36, 132)]
    public void PortraitLayoutKeepsHudBoardAndFeedbackInsideSafeArea(int width, int height, int side, int bottom, int top)
    {
        var safe = new Rect(side, bottom, width - side * 2, height - bottom - top);
        var boardSize = new Vector2(800, 1440);
        var fit = MatchFeedbackPanel.Fit(safe, boardSize);
        float boardTop = fit.center.y + boardSize.y * fit.scale * .5f;
        float boardBottom = fit.center.y - boardSize.y * fit.scale * .5f;
        Assert.That(fit.hud.yMin, Is.GreaterThan(boardTop));
        Assert.That(fit.panel.yMax, Is.LessThan(boardBottom));
        Assert.That(fit.scale, Is.InRange(.85f, 1.14f));
        Assert.That(safe.Contains(fit.panel.min), Is.True);
        Assert.That(safe.Contains(fit.hud.max), Is.True);
    }

    [UnityTest]
    public IEnumerator FallingTileOnlyRendersInsideTheBoard()
    {
        yield return SceneManager.LoadSceneAsync("02.Game");
        var game = Game.instance;
        yield return WaitFor(() => game.CanPlay, "initial drop");
        var topTile = game.blocks[2].blocks[0];
        var graphic = topTile.VisualRect.GetComponent<Image>();

        topTile.Show(1, 9);
        yield return null;
        Canvas.ForceUpdateCanvases();
        Assert.That(graphic.canvasRenderer.cull, Is.True, "a tile above the board is clipped before it enters the HUD");

        yield return WaitFor(topTile.CheckDropEnd, "test tile lands");
        Canvas.ForceUpdateCanvases();
        Assert.That(graphic.canvasRenderer.cull, Is.False, "the landed tile is visible in its original position");
    }

    [UnityTest]
    public IEnumerator EdgeMatchesKeepHudClearOfTheCameraSpacePuff()
    {
        yield return SceneManager.LoadSceneAsync("02.Game");
        var game = Game.instance;
        yield return WaitFor(() => game.CanPlay, "initial drop");
        var effect = game.GetComponent<MatchBurst>();
        var particles = Object.FindObjectOfType<Sfx>();

        effect.Play(game.blocks, new List<(int, int)> { (2, 3), (2, 4), (2, 5) }, (2, 4), false);
        Assert.That(particles.GetComponentsInChildren<ParticleSystem>(), Has.Length.EqualTo(1));

        effect.Play(game.blocks, new List<(int, int)> { (0, 0), (1, 0), (0, 1) }, (0, 0), false);
        Assert.That(particles.GetComponentsInChildren<ParticleSystem>(), Is.Empty,
            "the camera-space puff must not reach the HUD at the board edge");
        var layer = game.transform.Find("Game/Match Burst");
        Assert.That(layer.GetComponentsInChildren<Image>(), Is.Not.Empty,
            "the edge match still draws its clipped UI burst");
    }

    static IEnumerator WaitFor(Func<bool> condition, string phase, float timeout = 5f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
        Assert.That(condition(), Is.True, phase);
    }

    static void AssertInsideViewport(RectTransform rect, Canvas canvas, Vector3[] corners)
    {
        rect.GetWorldCorners(corners);
        var bounds = canvas.pixelRect;
        foreach (var corner in corners)
        {
            var point = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corner);
            Assert.That(point.x, Is.InRange(bounds.xMin - 1f, bounds.xMax + 1f), rect.name + " horizontal visibility");
            Assert.That(point.y, Is.InRange(bounds.yMin - 1f, bounds.yMax + 1f), rect.name + " vertical visibility");
        }
    }

    static void ClickVisibleButton(Button button, Vector2? position = null)
    {
        Canvas.ForceUpdateCanvases();
        var canvas = button.GetComponentInParent<Canvas>();
        var rect = (RectTransform)button.transform;
        var events = EventSystem.current;
        Assert.That(events, Is.Not.Null);
        var pointer = new PointerEventData(events)
        {
            position = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.TransformPoint(position ?? rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        events.RaycastAll(pointer, hits);
        Assert.That(hits, Is.Not.Empty, button.name + " raycast");
        var target = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
        Assert.That(target, Is.EqualTo(button.gameObject), "Raycast hits: " +
            string.Join(", ", hits.ConvertAll(hit => $"{hit.gameObject.name} (depth {hit.depth})")));
        Assert.That(ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler), Is.True);
    }

    static int[,] ReadBoard(Game game)
    {
        var board = new int[game.blocks.Length, game.blocks[0].blocks.Length];
        for (int x = 0; x < board.GetLength(0); x++)
        for (int y = 0; y < board.GetLength(1); y++)
        {
            var block = game.blocks[x].blocks[y];
            Assert.That(block.colorIndex, Is.InRange(1, game.MaxColorCount));
            Assert.That(block.Sprite, Is.Not.Null);
            board[x, y] = block.colorIndex;
        }
        return board;
    }

    static (int x, int y, int count) FindMatch(int[,] board)
    {
        for (int x = 0; x < board.GetLength(0); x++)
        for (int y = 0; y < board.GetLength(1); y++)
        {
            int count = MatchRules.FindGroup(board, x, y).Count;
            if (count >= MatchRules.MinimumGroupSize)
            {
                return (x, y, count);
            }
        }
        throw new AssertionException("Board has no group of three.");
    }
}
