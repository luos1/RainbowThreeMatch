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

public class Game : MonoBehaviour
{
    public static Game instance;
    public int MaxColorCount => Grade + 3;
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

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        var positions = PositionHelper.instance;
        boardData = new int[positions.XCount, positions.YCount];
        for (int x = 0; x < boardData.GetLength(0); x++)
        for (int y = 0; y < boardData.GetLength(1); y++)
        {
            blocks[x].blocks[y].SetPosition(x, y);
        }

        BeginRound();
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

        SetTimer(Mathf.Max(0f, remainingSeconds - Time.deltaTime));
        if (remainingSeconds <= 0f)
        {
            roundActive = false;
            matchBurst?.Clear();
            alphaScreen.SetActive(true);
            objGameOver.SetActive(true);
            soundEffects?.PlayRoundEnd();
        }
    }

    public void OnClickRetry()
    {
        if (boardData == null || roundActive || startingRound)
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
                matchFeedback?.ShowFailure(boardData[position.x, position.y], group.Count);
                soundEffects?.PlayMiss();
            }
            return;
        }

        matchFeedback?.StartMatch(boardData[position.x, position.y], group.Count);
        resolvingBoard = true;
        alphaScreen.SetActive(true);
        bool chained = previousClickMatched;
        if (chained)
        {
            SetTimer(Mathf.Min(timeSeconds, remainingSeconds + MatchRules.GetTimeBonus(group.Count)));
        }
        previousClickMatched = true;

        Score += MatchRules.GetScore(group.Count);
        ShowScore?.Invoke(Score);
        soundEffects?.PlayMatch(group.Count, chained);
        matchBurst?.Play(blocks, group, position, chained);
        foreach (var cell in group)
        {
            boardData[cell.x, cell.y] = 0;
            blocks[cell.x].blocks[cell.y].Pop();
        }

        StartCoroutine(RefillBoard());
    }

    IEnumerator StartRound()
    {
        RollBoard();
        yield return WaitForDrop();
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

    IEnumerator RefillBoard()
    {
        yield return null;

        // Neighbors cycle within the palette that was active when the player clicked.
        var changed = MatchRules.CycleNeighbors(boardData, MaxColorCount);
        foreach (var cell in changed)
        {
            var block = blocks[cell.x].blocks[cell.y];
            int nextColor = boardData[cell.x, cell.y];
            matchFeedback?.RecordTransition(block.colorIndex, nextColor);
            block.ChangeSprite(nextColor);
        }

        int nextGrade = MatchRules.GetGrade(Score, minGrade, maxGrade);
        if (Grade != nextGrade)
        {
            Grade = nextGrade;
            ShowGrade?.Invoke(Grade);
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

    int NextColor() => Random.Range(1, MaxColorCount + 1);

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
