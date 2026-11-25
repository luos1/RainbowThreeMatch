using System;
using System.Threading.Tasks;

namespace Rainbow.Services
{
    internal sealed class AccountProgress
    {
        private readonly IAuthService auth;
        private readonly ICloudSaveService local;
        private readonly ICloudSaveService cloud;

        internal AccountProgress(IAuthService auth, ICloudSaveService local, ICloudSaveService cloud = null)
        {
            this.auth = auth;
            this.local = local;
            this.cloud = cloud;
        }

        internal Task<SaveData> LoadLocal(string uid) => local.Load(uid);

        internal async Task<SaveData> Sync(string uid)
        {
            SaveData cached = await local.Load(uid);
            if (cloud == null || auth.CurrentUser?.Uid != uid)
                return cached;

            SaveData remote = cached == null ? await cloud.Load(uid) : await cloud.Save(uid, cached);
            return remote == null ? cached : await local.Save(uid, remote);
        }

        internal async Task RecordScore(string uid, int score)
        {
            SaveData cached = await local.Load(uid);
            if (cached != null && score <= cached.HighScore)
                return;

            await local.Save(uid, new SaveData(score));
            await Sync(uid);
        }

        internal Task<SaveData> KeepLinkedProgress(AuthUser linked, SaveData guestSave)
        {
            if (linked == null || linked.IsAnonymous || auth.CurrentUser?.Uid != linked.Uid)
                throw new InvalidOperationException("The linked Google account must be current.");

            return guestSave == null ? local.Load(linked.Uid) : local.Save(linked.Uid, guestSave);
        }
    }
}
