using System;
using System.Collections.Generic;

public static class MatchRules
{
    public const int MinimumGroupSize = 3;

    static readonly (int x, int y)[] Neighbors = { (-1, 0), (1, 0), (0, -1), (0, 1) };

    public static List<(int x, int y)> FindGroup(int[,] board, int x, int y)
    {
        var group = new List<(int x, int y)>();
        if (IsOnBoard(board, x, y) && board[x, y] > 0)
        {
            Visit(board, x, y, board[x, y], new bool[board.GetLength(0), board.GetLength(1)], group);
        }
        return group;
    }

    public static bool HasMatch(int[,] board)
    {
        int width = board.GetLength(0);
        int height = board.GetLength(1);
        var visited = new bool[width, height];
        var group = new List<(int x, int y)>();
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            if (visited[x, y] || board[x, y] == 0)
            {
                continue;
            }

            group.Clear();
            Visit(board, x, y, board[x, y], visited, group);
            if (group.Count >= MinimumGroupSize)
            {
                return true;
            }
        }

        return false;
    }

    public static void EnsureMatch(int[,] board)
    {
        if (HasMatch(board))
        {
            return;
        }

        int width = board.GetLength(0);
        int height = board.GetLength(1);
        // A fresh roll is playable even when its random colors have no match.
        if (height >= MinimumGroupSize)
        {
            board[0, height - 2] = board[0, height - 3] = board[0, height - 1];
        }
        else if (width >= MinimumGroupSize)
        {
            board[1, 0] = board[2, 0] = board[0, 0];
        }
        else if (width == 2 && height == 2)
        {
            board[0, 1] = board[1, 0] = board[0, 0];
        }
    }

    public static HashSet<(int x, int y)> CycleNeighbors(int[,] board, int paletteSize)
    {
        var changed = new HashSet<(int x, int y)>();
        for (int x = 0; x < board.GetLength(0); x++)
        for (int y = 0; y < board.GetLength(1); y++)
        {
            if (board[x, y] != 0)
            {
                continue;
            }

            foreach (var offset in Neighbors)
            {
                int neighborX = x + offset.x;
                int neighborY = y + offset.y;
                if (IsOnBoard(board, neighborX, neighborY) && board[neighborX, neighborY] > 0 &&
                    changed.Add((neighborX, neighborY)))
                {
                    board[neighborX, neighborY] = board[neighborX, neighborY] % paletteSize + 1;
                }
            }
        }
        return changed;
    }

    public static int[] RefillColumn(int[,] board, int x, Func<int> nextColor)
    {
        int height = board.GetLength(1);
        var dropHeights = new int[height];
        int write = height - 1;
        for (int read = height - 1; read >= 0; read--)
        {
            int color = board[x, read];
            if (color == 0)
            {
                continue;
            }

            board[x, write] = color;
            dropHeights[write] = write - read;
            write--;
        }

        int added = write + 1;
        for (; write >= 0; write--)
        {
            board[x, write] = nextColor();
            dropHeights[write] = added;
        }
        return dropHeights;
    }

    public static int GetScore(int count) =>
        count < MinimumGroupSize ? 0 : 10 + (count - MinimumGroupSize) * (count - MinimumGroupSize);

    public static int GetGrade(int score, int minGrade, int maxGrade) =>
        Math.Min(minGrade + score / 1000, maxGrade - 1);

    public static int GetTimeBonus(int count) => count - MinimumGroupSize + 1;

    static void Visit(int[,] board, int x, int y, int color, bool[,] visited, List<(int x, int y)> group)
    {
        if (!IsOnBoard(board, x, y) || visited[x, y] || board[x, y] != color)
        {
            return;
        }

        visited[x, y] = true;
        group.Add((x, y));
        foreach (var offset in Neighbors)
        {
            Visit(board, x + offset.x, y + offset.y, color, visited, group);
        }
    }

    static bool IsOnBoard(int[,] board, int x, int y) =>
        x >= 0 && x < board.GetLength(0) && y >= 0 && y < board.GetLength(1);
}
