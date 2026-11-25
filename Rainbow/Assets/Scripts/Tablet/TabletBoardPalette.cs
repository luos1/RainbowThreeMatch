using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Game
{
    internal const float TabletSupplyPreviewSeconds = .5f;
    const float TabletGuideNoticeSeconds = 1.35f;

    readonly List<Block> supplyMarks = new List<Block>(3);
    bool seededTabletHealing, seededTabletShield, supplyPreview;
    int supplyColor, supplyRow, unlockedColor, observedReinforcements;
    int lastTabletMissFrame = -1;
    float supplyUntil, unlockUntil, arrivalUntil;

    void ResetTabletBoardGuidance()
    {
        seededTabletHealing = seededTabletShield = false;
        unlockedColor = observedReinforcements = 0;
        unlockUntil = arrivalUntil = 0;
        lastTabletMissFrame = -1;
        ClearTabletSupply();
    }

    void SyncTabletPalette()
    {
        var combat = tablet.Combat;
        float now = combat.Elapsed;
        if (combat.PaletteSize != tabletPalette)
        {
            tabletPalette = combat.PaletteSize;
            Grade = tabletPalette - 3;
            ShowGrade?.Invoke(Grade);
            matchFeedback.SetPalette(tabletPalette);
            unlockedColor = tabletPalette;
            unlockUntil = now + TabletGuideNoticeSeconds;
        }

        if (supplyColor != 0)
        {
            if (now < supplyUntil) return;
            if (supplyPreview)
            {
                for (int x = 1; x <= 3; x++)
                {
                    if (boardData[x, supplyRow] == supplyColor) continue;
                    boardData[x, supplyRow] = supplyColor;
                    blocks[x].blocks[supplyRow].ChangeSprite(supplyColor);
                }
                FinishTabletSupply(now);
            }
            else
                ClearTabletSupply();
        }

        if (supplyColor == 0 && tabletPalette >= 4 && !seededTabletHealing)
            BeginTabletSupply(4, boardData.GetLength(1) / 2, now);
        else if (supplyColor == 0 && tabletPalette >= 6 && !seededTabletShield)
            BeginTabletSupply(6, boardData.GetLength(1) / 2 + 1, now);
    }

    void BeginTabletSupply(int color, int row, float now)
    {
        supplyColor = color;
        supplyRow = row;
        for (int x = 0; x < boardData.GetLength(0); x++)
        for (int y = 0; y < boardData.GetLength(1); y++)
        {
            if (boardData[x, y] != color) continue;
            var group = MatchRules.FindGroup(boardData, x, y);
            if (group.Count < MatchRules.MinimumGroupSize) continue;
            for (int i = 0; i < MatchRules.MinimumGroupSize; i++)
                AddTabletSupplyMark(group[i].x, group[i].y);
            FinishTabletSupply(now);
            return;
        }

        for (int x = 1; x <= 3; x++) AddTabletSupplyMark(x, row);
        supplyPreview = true;
        supplyUntil = now + TabletSupplyPreviewSeconds;
    }

    void AddTabletSupplyMark(int x, int y)
    {
        var block = blocks[x].blocks[y];
        supplyMarks.Add(block);
        block.MarkTabletSupply(supplyColor);
    }

    void FinishTabletSupply(float now)
    {
        if (supplyColor == 4) seededTabletHealing = true;
        else seededTabletShield = true;
        supplyPreview = false;
        supplyUntil = now + TabletGuideNoticeSeconds;
    }

    void ClearTabletSupply()
    {
        foreach (var block in supplyMarks) block.MarkTabletSupply(0);
        supplyMarks.Clear();
        supplyColor = 0;
        supplyPreview = false;
    }

    bool HandleTabletMiss((int x, int y) position, int connected)
    {
        if (connected == 0 || lastTabletMissFrame == Time.frameCount) return false;
        if (!MatchRules.HasMatch(boardData))
        {
            resolvingBoard = true;
            alphaScreen.SetActive(true);
            ClearTabletSupply();
            matchFeedback.ShowTabletNoMoves();
            StartCoroutine(RebuildTabletBoard());
            return false;
        }

        lastTabletMissFrame = Time.frameCount;
        var outcome = tablet.Combat.RegisterMiss();
        matchFeedback.ShowTabletMiss(boardData[position.x, position.y], connected, outcome);
        UpdateTabletBoardGuidance();
        return true;
    }

    IEnumerator RebuildTabletBoard()
    {
        RollBoard();
        yield return WaitForDrop();
        FinishDrop();
    }

    void UpdateTabletBoardGuidance()
    {
        var combat = tablet.Combat;
        if (combat.ReinforcementsArrived != observedReinforcements)
        {
            observedReinforcements = combat.ReinforcementsArrived;
            arrivalUntil = combat.Elapsed + TabletGuideNoticeSeconds;
        }
        matchFeedback.UpdateTabletBoardGuidance(combat.Elapsed, supplyColor, supplyPreview,
            combat.Elapsed < unlockUntil ? unlockedColor : 0, combat.FailureStreak, combat.ReinforcementsPending,
            combat.Elapsed < arrivalUntil);
    }
}
