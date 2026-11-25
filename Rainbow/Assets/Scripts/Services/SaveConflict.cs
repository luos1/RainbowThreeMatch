using System;

namespace Rainbow.Services
{
    public static class SaveConflict
    {
        public static SaveData Resolve(SaveData first, SaveData second)
        {
            if (first == null)
                return second?.Clone();
            if (second == null)
                return first.Clone();

            long updatedAt = Math.Max(first.UpdatedAt, second.UpdatedAt);
            string displayName;
            if (first.UpdatedAt > second.UpdatedAt)
                displayName = first.DisplayName;
            else if (second.UpdatedAt > first.UpdatedAt)
                displayName = second.DisplayName;
            else
                displayName = string.CompareOrdinal(first.DisplayName, second.DisplayName) >= 0
                    ? first.DisplayName : second.DisplayName;

            return new SaveData(Math.Max(first.HighScore, second.HighScore), updatedAt, displayName);
        }
    }
}
