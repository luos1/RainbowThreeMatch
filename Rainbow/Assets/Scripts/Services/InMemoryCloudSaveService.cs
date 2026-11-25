using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rainbow.Services
{
    public sealed class InMemoryCloudSaveService : ICloudSaveService
    {
        private readonly Dictionary<string, SaveData> saves = new Dictionary<string, SaveData>(StringComparer.Ordinal);

        public Task<SaveData> Save(string uid, SaveData data)
        {
            SaveData.ValidateUid(uid);
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            saves.TryGetValue(uid, out SaveData existing);
            SaveData resolved = SaveConflict.Resolve(existing, data);
            saves[uid] = resolved;
            return Task.FromResult(resolved.Clone());
        }

        public Task<SaveData> Load(string uid)
        {
            SaveData.ValidateUid(uid);
            saves.TryGetValue(uid, out SaveData value);
            return Task.FromResult(value?.Clone());
        }

    }
}
