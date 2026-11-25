using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#if RAINBOW_FIREBASE
using Firebase.Auth;
using Firebase.Firestore;
#endif

namespace Rainbow.Services
{
    public sealed class FirestoreCloudSaveService : ICloudSaveService
    {
#if RAINBOW_FIREBASE
        private FirebaseFirestore database;

        public async Task<SaveData> Save(string uid, SaveData data)
        {
            SaveData.ValidateUid(uid);
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            SaveData candidate = data.Clone();
            await FirebaseRuntime.EnsureReady();
            EnsureOwner(uid);

            DocumentReference document = Database.Document("users/" + uid);
            return await Database.RunTransactionAsync<SaveData>(async transaction =>
            {
                EnsureOwner(uid);
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(document);
                SaveData remote = snapshot.Exists ? FromSnapshot(snapshot) : null;
                SaveData resolved = SaveConflict.Resolve(remote, candidate);
                if (!resolved.Equals(remote))
                    transaction.Set(document, ToFields(resolved));
                return resolved;
            });
        }

        public async Task<SaveData> Load(string uid)
        {
            SaveData.ValidateUid(uid);
            await FirebaseRuntime.EnsureReady();
            EnsureOwner(uid);
            DocumentSnapshot snapshot = await Database.Document("users/" + uid).GetSnapshotAsync(Source.Server);
            return snapshot.Exists ? FromSnapshot(snapshot) : null;
        }

        private FirebaseFirestore Database => database ?? (database = FirebaseFirestore.DefaultInstance);

        private static void EnsureOwner(string uid)
        {
            FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
            if (user == null || !string.Equals(user.UserId, uid, StringComparison.Ordinal))
                throw new InvalidOperationException("Cloud saves can only access the signed-in user's record.");
        }

        private static Dictionary<string, object> ToFields(SaveData data) => new Dictionary<string, object>
        {
            ["highScore"] = data.HighScore,
            ["updatedAt"] = data.UpdatedAt,
            ["displayName"] = data.DisplayName
        };

        private static SaveData FromSnapshot(DocumentSnapshot snapshot)
        {
            long highScore = snapshot.GetValue<long>("highScore");
            long updatedAt = snapshot.GetValue<long>("updatedAt");
            string displayName = snapshot.ContainsField("displayName")
                ? snapshot.GetValue<string>("displayName") : string.Empty;
            if (highScore < 0 || highScore > int.MaxValue)
                throw new InvalidOperationException("The stored high score exceeds the supported range.");
            return new SaveData((int)highScore, updatedAt, displayName);
        }
#else
        public Task<SaveData> Save(string uid, SaveData data)
        {
            SaveData.ValidateUid(uid);
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            throw MissingFirebase();
        }

        public Task<SaveData> Load(string uid)
        {
            SaveData.ValidateUid(uid);
            throw MissingFirebase();
        }

        private static NotSupportedException MissingFirebase() => new NotSupportedException(
            "Firestore requires the pinned Firebase packages and the RAINBOW_FIREBASE compile define.");
#endif
    }
}
