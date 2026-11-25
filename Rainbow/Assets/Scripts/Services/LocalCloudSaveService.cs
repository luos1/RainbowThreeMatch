using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Rainbow.Services
{
    public sealed class LocalCloudSaveService : ICloudSaveService
    {
        private readonly string keyPrefix;

        [Serializable]
        private sealed class StoredSave
        {
            public int highScore;
            public long updatedAt;
            public string displayName;
        }

        public LocalCloudSaveService(string keyPrefix = "RainbowThreeMatch.CloudSave")
        {
            if (string.IsNullOrWhiteSpace(keyPrefix))
                throw new ArgumentException("A storage prefix is required.", nameof(keyPrefix));
            this.keyPrefix = keyPrefix + ".";
        }

        public async Task<SaveData> Save(string uid, SaveData data)
        {
            SaveData.ValidateUid(uid);
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            SaveData previous = await Load(uid);
            SaveData resolved = SaveConflict.Resolve(previous, data);
            if (resolved.Equals(previous))
                return resolved;
            var stored = new StoredSave
            {
                highScore = resolved.HighScore,
                updatedAt = resolved.UpdatedAt,
                displayName = resolved.DisplayName
            };
            PlayerPrefs.SetString(Key(uid), JsonUtility.ToJson(stored));
            PlayerPrefs.Save();
            return resolved;
        }

        public Task<SaveData> Load(string uid)
        {
            SaveData.ValidateUid(uid);
            string json = PlayerPrefs.GetString(Key(uid), string.Empty);
            if (string.IsNullOrEmpty(json))
                return Task.FromResult<SaveData>(null);

            StoredSave stored;
            try
            {
                stored = JsonUtility.FromJson<StoredSave>(json);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("The local cloud-save cache is invalid.", exception);
            }

            if (stored == null)
                throw new InvalidOperationException("The local cloud-save cache is invalid.");
            return Task.FromResult(new SaveData(stored.highScore, stored.updatedAt, stored.displayName));
        }

        public void Delete(string uid)
        {
            SaveData.ValidateUid(uid);
            PlayerPrefs.DeleteKey(Key(uid));
            PlayerPrefs.Save();
        }

        private string Key(string uid) => keyPrefix + uid;
    }
}
