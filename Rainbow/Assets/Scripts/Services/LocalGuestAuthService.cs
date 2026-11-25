using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Rainbow.Services
{
    public sealed class LocalGuestAuthService : IAuthService
    {
        private const string DefaultStoragePrefix = "Rainbow.Auth.Guest.";
        private readonly string uidKey;
        private readonly string activeKey;

        public AuthUser CurrentUser { get; private set; }
        public event Action<AuthUser> StateChanged;

        public LocalGuestAuthService(string storagePrefix = DefaultStoragePrefix)
        {
            if (string.IsNullOrWhiteSpace(storagePrefix))
                throw new ArgumentException("A storage prefix is required.", nameof(storagePrefix));

            uidKey = storagePrefix + "uid";
            activeKey = storagePrefix + "active";
        }

        public Task Initialize()
        {
            string uid = PlayerPrefs.GetString(uidKey, null);
            SetCurrent(PlayerPrefs.GetInt(activeKey, 0) == 1 && !string.IsNullOrWhiteSpace(uid)
                ? new AuthUser(uid, true)
                : null);
            return Task.CompletedTask;
        }

        public Task<AuthUser> SignInAnonymously()
        {
            string uid = PlayerPrefs.GetString(uidKey, null);
            if (string.IsNullOrWhiteSpace(uid))
            {
                uid = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(uidKey, uid);
            }

            PlayerPrefs.SetInt(activeKey, 1);
            PlayerPrefs.Save();
            SetCurrent(new AuthUser(uid, true));
            return Task.FromResult(CurrentUser);
        }

        public Task<AuthUser> SignInWithGoogle(string idToken) => FirebaseRequired<AuthUser>();
        public Task<AuthUser> LinkGuestToGoogle(string idToken) => FirebaseRequired<AuthUser>();

        public void SignOut()
        {
            PlayerPrefs.SetInt(activeKey, 0);
            PlayerPrefs.Save();
            SetCurrent(null);
        }

        private static Task<T> FirebaseRequired<T>() => Task.FromException<T>(new NotSupportedException(
            "Google authentication requires the Firebase packages and the RAINBOW_FIREBASE compile define."));

        private void SetCurrent(AuthUser user)
        {
            if (Equals(CurrentUser, user))
                return;

            CurrentUser = user;
            StateChanged?.Invoke(user);
        }
    }
}
