using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class GameAudioTests
{
    [UnityTest]
    public IEnumerator BoardPressesUseOneVoiceForMissMatchAndChain()
    {
        yield return SceneManager.LoadSceneAsync("02.Game");
        var game = Game.instance;
        yield return WaitFor(() => game.CanPlay);
        var source = Object.FindObjectOfType<Sfx>().GetComponent<AudioSource>();
        Assert.That(source, Is.Not.Null, "Sfx owns a cue source in the actual game scene");
        Assert.That(source.clip, Is.Null, "initial scene load is silent");

        var board = (int[,])typeof(Game).GetField("boardData", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game);
        for (int x = 0; x < board.GetLength(0); x++)
        for (int y = 0; y < board.GetLength(1); y++)
            Set(game, board, x, y, (x + 2 * y) % 3 + 1);
        yield return null;
        for (int i = 0; i < 20; i++)
            Tap(game, 0, 0);
        Assert.That(source.clip.name, Is.EqualTo("error_004"));
        Assert.That(game.Score, Is.Zero);
        Assert.That(source.spatialBlend, Is.Zero);

        Set(game, board, 1, 0, 1);
        Set(game, board, 0, 1, 1);
        Set(game, board, 1, 1, 2);
        for (int x = 2; x < 5; x++)
        for (int y = 4; y < 9; y++)
            Set(game, board, x, y, 2);
        yield return null;
        Tap(game, 0, 0);
        Assert.That(source.clip.name, Is.EqualTo("confirmation_001"));
        Assert.That(game.Score, Is.EqualTo(10));
        for (int i = 0; i < 20; i++)
            Tap(game, 3, 7);
        Assert.That(source.clip.name, Is.EqualTo("confirmation_001"));
        Assert.That(game.Score, Is.EqualTo(10));
        yield return WaitFor(() => game.CanPlay);
        Tap(game, 3, 7);
        Assert.That(source.clip.name, Is.EqualTo("confirmation_004"));
        Assert.That(source.volume, Is.InRange(0f, .42f));
        Assert.That(source.pitch, Is.InRange(.95f, 1.101f));
        Assert.That(Object.FindObjectsOfType<AudioSource>(), Has.Length.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator TimeoutRetryAndSceneExitRenderDistinctCuesAndReleaseTheVoice()
    {
        yield return SceneManager.LoadSceneAsync("02.Game");
        var game = Game.instance;
        yield return WaitFor(() => game.CanPlay);
        var source = Object.FindObjectOfType<Sfx>().GetComponent<AudioSource>();
        Assert.That(source, Is.Not.Null);
        typeof(Game).GetField("remainingSeconds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(game, 0f);
        yield return WaitFor(() => game.TimeSeconds == 0);
        Assert.That(source.clip.name, Is.EqualTo("error_006"));
        var retry = Array.Find(Object.FindObjectsOfType<Button>(true), button =>
            button.onClick.GetPersistentEventCount() > 0 && button.onClick.GetPersistentMethodName(0) == nameof(Game.OnClickRetry));
        Assert.That(retry.gameObject.activeInHierarchy, Is.True);
        retry.onClick.Invoke();
        Assert.That(source.clip.name, Is.EqualTo("click_001"));
        Assert.That(game.Score, Is.Zero);
        Assert.That(game.TimeSeconds, Is.EqualTo(60));
        retry.onClick.Invoke();
        Assert.That(source.clip.name, Is.EqualTo("click_001"));
        yield return SceneManager.LoadSceneAsync("00.Loading");
        Assert.That(source == null, Is.True);
        Assert.That(Object.FindObjectsOfType<AudioSource>(), Is.Empty);
    }

    static void Tap(Game game, int x, int y) => game.blocks[x].blocks[y].GetComponent<Button>().onClick.Invoke();

    static void Set(Game game, int[,] board, int x, int y, int color)
    {
        board[x, y] = color;
        game.blocks[x].blocks[y].Show(color, 0);
    }

    static IEnumerator WaitFor(Func<bool> condition)
    {
        double end = Time.realtimeSinceStartupAsDouble + 8;
        while (!condition() && Time.realtimeSinceStartupAsDouble < end)
            yield return null;
        Assert.That(condition(), Is.True);
    }
}
