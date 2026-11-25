using System;

namespace Rainbow.Services
{
    [Serializable]
    public sealed class SaveData : IEquatable<SaveData>
    {
        private int highScore;
        private long updatedAt;
        private string displayName;

        public int HighScore
        {
            get => highScore;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "High score cannot be negative.");
                highScore = value;
            }
        }

        public long UpdatedAt
        {
            get => updatedAt;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Updated time cannot be negative.");
                updatedAt = value;
            }
        }

        public string DisplayName
        {
            get => displayName;
            set
            {
                string normalized = value ?? string.Empty;
                if (normalized.Length > 80)
                    throw new ArgumentException("Display name cannot exceed 80 characters.", nameof(value));
                displayName = normalized;
            }
        }

        public SaveData(int highScore = 0, long updatedAt = 0, string displayName = "")
        {
            HighScore = highScore;
            UpdatedAt = updatedAt;
            DisplayName = displayName;
        }

        public SaveData Clone() => new SaveData(HighScore, UpdatedAt, DisplayName);

        internal static void ValidateUid(string uid)
        {
            if (string.IsNullOrWhiteSpace(uid) || uid.Contains("/") || uid == "." || uid == "..")
                throw new ArgumentException("A user ID must be a valid document ID.", nameof(uid));
        }

        public bool Equals(SaveData other) => other != null
            && HighScore == other.HighScore
            && UpdatedAt == other.UpdatedAt
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as SaveData);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = HighScore;
                hash = (hash * 397) ^ UpdatedAt.GetHashCode();
                return (hash * 397) ^ StringComparer.Ordinal.GetHashCode(DisplayName);
            }
        }
    }
}
