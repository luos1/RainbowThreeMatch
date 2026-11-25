using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[Serializable]
public class HBlocks
{
    public int index;
    public Block[] blocks;
}

public partial class Game : MonoBehaviour
{
    public static Game instance;
    public int MaxColorCount => tablet == null ? Grade + 3 : tabletPalette;
    internal bool IsTablet => tablet != null;
    public int Score { get; private set; }
    public int Grade { get; private set; }
    public int TimeSeconds { get; private set; }
    public bool CanPlay => roundActive && !resolvingBoard;

    [SerializeField] int maxGrade = 5;
    [SerializeField] int minGrade;
    [SerializeField] int timeSeconds;
    [SerializeField] Image screen;
    [SerializeField] GameObject alphaScreen;
    [SerializeField] GameObject objGameOver;
    [SerializeField] MatchBurst matchBurst;
    [SerializeField] MatchFeedbackPanel matchFeedback;
    [SerializeField] Sfx soundEffects;
    public HBlocks[] blocks;

    public event Action<int> ShowScore;
    public event Action<int> ShowGrade;
    public event Action<int> ShowTimer;

    int[,] boardData;
    float remainingSeconds;
    bool roundActive;
    bool startingRound;
    bool resolvingBoard;
    bool previousClickMatched;
    TabletScene tablet;
    int tabletPalette;

    void Awake()
    {
        instance = this;
        tablet = GetComponent<TabletScene>();
    }

    void Start()
    {
        if (tablet != null)
        {
            tablet.Setup(this, matchFeedback);
            matchBurst?.UseCanvasOnly();
            var blocker = (RectTransform)alphaScreen.transform;
            blocker.SetParent(transform.Find("Game"), false);
            blocker.anchorMin = Vector2.zero;
            blocker.anchorMax = Vector2.one;
            blocker.offsetMin = blocker.offsetMax = Vector2.zero;
        }
        var positions = PositionHelper.instance;
        boardData = new int[positions.XCount, positions.YCount];
        for (int x = 0; x < boardData.GetLength(0); x++)
        for (int y = 0; y < boardData.GetLength(1); y++)
        {
            var block = blocks[x].blocks[y];
            block.SetPosition(x, y);
            if (tablet != null)
            {
                block.UseTabletDropTiming();
                block.UseTabletGlyphs(tablet.TileGlyphs);
            }
        }

        if (tablet == null)
            BeginRound();
        else
        {
            Grade = minGrade;
            tabletPalette = 3;
            alphaScreen.SetActive(true);
            objGameOver.SetActive(false);
            SetTimer(timeSeconds);
            RollBoard();
            foreach (var column in blocks)
            foreach (var block in column.blocks)
                block.Settle();
            matchFeedback.ResetRound(MaxColorCount);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    void Update()
    {
        if (!roundActive)
        {
            return;
        }

        if (tablet != null)
        {
            tablet.Tick(Time.deltaTime);
            SetTimer(tablet.Combat.Remaining);
            if (tablet.Combat.Result != TabletResult.Playing)
                EndRound();
            else
            {
                if (!resolvingBoard) SyncTabletPalette();
                UpdateTabletBoardGuidance();
            }
            return;
        }

        SetTimer(Mathf.Max(0f, remainingSeconds - Time.deltaTime));
        if (remainingSeconds <= 0f)
            EndRound();
    }

    void EndRound()
    {
        roundActive = false;
        matchBurst?.Clear();
        alphaScreen.SetActive(true);
        if (tablet == null)
            objGameOver.SetActive(true);
        else
        {
            StopAllCoroutines();
            foreach (var column in blocks)
            foreach (var block in column.blocks)
                block.Settle();
            ClearTabletSupply();
            matchFeedback.ClearTabletGuidance();
            matchFeedback.StopPulse();
            tablet.FinishFlight();
        }
        soundEffects?.PlayRoundEnd();
    }

    public void OnClickRetry()
    {
        if (boardData == null || roundActive || startingRound || (tablet != null && !tablet.CanLaunch))
        {
            return;
        }

        BeginRound();
        soundEffects?.PlayRetry();
    }

    void BeginRound()
    {
        StopAllCoroutines();
        matchBurst?.ResetRound();
        startingRound = true;
        resolvingBoard = true;
        previousClickMatched = false;
        alphaScreen.SetActive(true);
        screen.gameObject.SetActive(false);
        objGameOver.SetActive(false);

        Score = 0;
        Grade = minGrade;
        tabletPalette = 3;
        if (tablet != null) ResetTabletBoardGuidance();
        tablet?.ResetRound();
        matchFeedback?.ResetRound(MaxColorCount);
        ShowScore?.Invoke(Score);
        ShowGrade?.Invoke(Grade);
        SetTimer(timeSeconds);
        StartCoroutine(StartRound());
    }

    public void Check((int x, int y) position)
    {
        if (!CanPlay)
        {
            return;
        }

        var group = MatchRules.FindGroup(boardData, position.x, position.y);
        if (group.Count < MatchRules.MinimumGroupSize)
        {
            previousClickMatched = false;
            if (group.Count > 0)
            {
                if (tablet == null)
                {
                    matchFeedback?.ShowFailure(boardData[position.x, position.y], group.Count);
                    soundEffects?.PlayMiss();
                }
                else if (HandleTabletMiss(position, group.Count))
                    soundEffects?.PlayMiss();
            }
            return;
        }

        int color = boardData[position.x, position.y];
        int paletteAtClick = MaxColorCount;
        if (tablet != null) ClearTabletSupply();
        matchFeedback?.StartMatch(color, group.Count);
        resolvingBoard = true;
        alphaScreen.SetActive(true);
        bool chained = previousClickMatched;
        if (chained && tablet == null)
        {
            SetTimer(Mathf.Min(timeSeconds, remainingSeconds + MatchRules.GetTimeBonus(group.Count)));
        }
        previousClickMatched = true;

        Score += MatchRules.GetScore(group.Count);
        ShowScore?.Invoke(Score);
        if (tablet != null)
        {
            matchFeedback.ShowTabletResult(tablet.Match(color, group.Count), color);
            UpdateTabletBoardGuidance();
        }
        soundEffects?.PlayMatch(group.Count, chained);
        matchBurst?.Play(blocks, group, position, chained);
        foreach (var cell in group)
        {
            boardData[cell.x, cell.y] = 0;
            blocks[cell.x].blocks[cell.y].Pop();
        }

        StartCoroutine(RefillBoard(paletteAtClick));
    }

    IEnumerator StartRound()
    {
        float launchEnd = Time.unscaledTime + TabletScene.LaunchTime;
        RollBoard();
        yield return WaitForDrop();
        if (tablet != null)
        {
            while (Time.unscaledTime < launchEnd)
                yield return null;
            tablet.StartFlight();
        }
        startingRound = false;
        roundActive = true;
        FinishDrop();
    }

    void RollBoard()
    {
        int width = boardData.GetLength(0);
        int height = boardData.GetLength(1);
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            boardData[x, y] = NextColor();
        }

        MatchRules.EnsureMatch(boardData);
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            blocks[x].blocks[y].Show(boardData[x, y], height);
        }
    }

    IEnumerator RefillBoard(int paletteAtClick)
    {
        yield return null;

        // Neighbors cycle within the palette that was active when the player clicked.
        var changed = MatchRules.CycleNeighbors(boardData, paletteAtClick);
        foreach (var cell in changed)
        {
            var block = blocks[cell.x].blocks[cell.y];
            int nextColor = boardData[cell.x, cell.y];
            matchFeedback?.RecordTransition(block.colorIndex, nextColor);
            block.ChangeSprite(nextColor);
        }

        if (tablet == null)
        {
            int nextGrade = MatchRules.GetGrade(Score, minGrade, maxGrade);
            if (Grade != nextGrade)
            {
                Grade = nextGrade;
                ShowGrade?.Invoke(Grade);
            }
        }
        matchFeedback?.FinishMatch(changed.Count, MaxColorCount);

        yield return null;

        int width = boardData.GetLength(0);
        int height = boardData.GetLength(1);
        for (int x = 0; x < width; x++)
        {
            var dropHeights = MatchRules.RefillColumn(boardData, x, NextColor);
            for (int y = 0; y < height; y++)
            {
                if (dropHeights[y] > 0)
                {
                    blocks[x].blocks[y].Show(boardData[x, y], dropHeights[y]);
                }
            }
        }

        yield return WaitForDrop();
        if (roundActive && !MatchRules.HasMatch(boardData))
        {
            RollBoard();
            yield return WaitForDrop();
        }
        FinishDrop();
    }

    int NextColor()
    {
        int count = MaxColorCount;
        if (tablet == null || count < 4)
            return Random.Range(1, count + 1);
        if (Random.value < .20f)
            return count >= 6 && Random.value < .5f ? 6 : 4;
        int attack = Random.Range(1, count - (count >= 6 ? 2 : 1) + 1);
        return attack <= 3 ? attack : attack == 4 ? 5 : 7;
    }

    IEnumerator WaitForDrop()
    {
        yield return new WaitUntil(() =>
        {
            foreach (var column in blocks)
            foreach (var block in column.blocks)
            {
                if (!block.CheckDropEnd())
                {
                    return false;
                }
            }
            return true;
        });
    }

    void FinishDrop()
    {
        if (tablet != null && roundActive) SyncTabletPalette();
        resolvingBoard = false;
        alphaScreen.SetActive(!roundActive);
    }

    void SetTimer(float seconds)
    {
        remainingSeconds = seconds;
        int displaySeconds = Mathf.CeilToInt(seconds);
        if (TimeSeconds != displaySeconds)
        {
            TimeSeconds = displaySeconds;
            ShowTimer?.Invoke(TimeSeconds);
        }
    }
}
