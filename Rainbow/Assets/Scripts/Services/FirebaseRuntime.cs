using System;
using System.Threading.Tasks;

namespace Rainbow.Services
{
    /// <summary>One dependency check shared by Auth and Firestore; never logs credentials.</summary>
    public static class FirebaseRuntime
    {
        private static Task ready;
        public static Task EnsureReady()
        {
            if (ready == null || ready.IsFaulted || ready.IsCanceled)
                ready = Initialize();
            return ready;
        }

        private static async Task Initialize()
        {
#if RAINBOW_FIREBASE
            var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != Firebase.DependencyStatus.Available)
                throw new InvalidOperationException("Firebase dependencies unavailable: " + status);
            // Access only after dependency resolution. Local config is supplied by the owner.
            var app = Firebase.FirebaseApp.DefaultInstance;
#else
            await Task.CompletedTask;
            throw new NotSupportedException("Import the pinned Firebase SDK to enable RAINBOW_FIREBASE.");
#endif
        }
    }
}
