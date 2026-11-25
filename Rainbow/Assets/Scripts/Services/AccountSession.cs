using System;
using UnityEngine;

namespace Rainbow.Services
{
    internal static class AccountSession
    {
        internal static IAuthService Auth { get; private set; }
        internal static AccountProgress Progress { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Reset()
        {
#if RAINBOW_FIREBASE && (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            SetServices(new FirebaseAuthService(), new LocalCloudSaveService(), new FirestoreCloudSaveService());
#else
            SetServices(new LocalGuestAuthService(), new LocalCloudSaveService());
#endif
        }

        internal static void SetServices(IAuthService auth, ICloudSaveService local, ICloudSaveService cloud = null)
        {
            (Auth as IDisposable)?.Dispose();
            Auth = auth;
            Progress = new AccountProgress(auth, local, cloud);
        }
    }
}
