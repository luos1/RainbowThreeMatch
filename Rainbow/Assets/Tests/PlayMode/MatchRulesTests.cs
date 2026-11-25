using NUnit.Framework;

public class MatchRulesTests
{
    [Test]
    public void MatchUsesOrthogonalConnectivityAndIgnoresEmptyCells()
    {
        int[,] board = { { 1, 1, 0 }, { 2, 1, 2 }, { 1, 2, 1 } };

        CollectionAssert.AreEquivalent(new[] { (0, 0), (0, 1), (1, 1) }, MatchRules.FindGroup(board, 0, 0));
        Assert.That(MatchRules.FindGroup(board, 2, 2).Count, Is.EqualTo(1));
        Assert.That(MatchRules.FindGroup(board, 0, 2), Is.Empty);
        Assert.That(MatchRules.FindGroup(board, -1, 0), Is.Empty);
    }

    [Test]
    public void NeighborsThatTouchMultipleHolesCycleOnlyOnce()
    {
        int[,] board = { { 1, 0, 2 }, { 0, 3, 2 } };

        var changed = MatchRules.CycleNeighbors(board, 3);

        CollectionAssert.AreEquivalent(new[] { (0, 0), (0, 2), (1, 1) }, changed);
        Assert.That(board[0, 0], Is.EqualTo(2));
        Assert.That(board[1, 1], Is.EqualTo(1));
        Assert.That(board[1, 2], Is.EqualTo(2));
    }

    [Test]
    public void RefillPreservesOrderAndCalculatesEachTilesDropDistance()
    {
        int[,] board = { { 1, 0, 2, 0, 3 } };
        int nextColor = 4;

        var heights = MatchRules.RefillColumn(board, 0, () => nextColor++);

        Assert.That(board, Is.EqualTo(new int[,] { { 5, 4, 1, 2, 3 } }));
        CollectionAssert.AreEqual(new[] { 2, 2, 2, 1, 0 }, heights);
    }

    [Test]
    public void RefillOfEmptyColumnGivesAllNewTilesTheSameDistance()
    {
        var board = new int[1, 4];
        var heights = MatchRules.RefillColumn(board, 0, () => 1);

        Assert.That(board, Is.EqualTo(new int[,] { { 1, 1, 1, 1 } }));
        CollectionAssert.AreEqual(new[] { 4, 4, 4, 4 }, heights);
    }

    [Test]
    public void EnsureMatchKeepsExistingMovesAndRepairsADeadBoard()
    {
        int[,] playable = { { 1, 1, 1 }, { 2, 3, 2 } };
        int[,] dead = { { 1, 2, 3 }, { 2, 3, 1 } };

        Assert.That(MatchRules.HasMatch(playable), Is.True);
        Assert.That(MatchRules.HasMatch(dead), Is.False);
        MatchRules.EnsureMatch(playable);
        MatchRules.EnsureMatch(dead);

        Assert.That(playable, Is.EqualTo(new int[,] { { 1, 1, 1 }, { 2, 3, 2 } }));
        Assert.That(MatchRules.HasMatch(dead), Is.True);
        Assert.That(MatchRules.FindGroup(dead, 0, 2).Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(dead[1, 1], Is.EqualTo(3));
    }

    [TestCase(3, 10, 1)]
    [TestCase(4, 11, 2)]
    [TestCase(9, 46, 7)]
    public void ScoringAndTimeBonusFollowGroupSize(int count, int score, int seconds)
    {
        Assert.That(MatchRules.GetScore(count), Is.EqualTo(score));
        Assert.That(MatchRules.GetTimeBonus(count), Is.EqualTo(seconds));
    }

    [TestCase(999, 0)]
    [TestCase(1000, 1)]
    [TestCase(3999, 3)]
    [TestCase(5000, 4)]
    public void DifficultyIncreasesEveryThousandPointsAndClamps(int score, int grade)
    {
        Assert.That(MatchRules.GetGrade(score, 0, 5), Is.EqualTo(grade));
    }
}
