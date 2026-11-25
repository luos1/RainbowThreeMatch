using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class GameSceneTests
{
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
            button.onClick.Invoke();
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
            retry.onClick.Invoke();
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

    static IEnumerator WaitFor(Func<bool> condition, string phase, float timeout = 5f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }
        Assert.That(condition(), Is.True, phase);
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
