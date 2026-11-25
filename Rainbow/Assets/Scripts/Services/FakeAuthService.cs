using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rainbow.Services
{
    public sealed class FakeAuthService : IAuthService
    {
        private readonly Dictionary<string, AuthUser> googleUsers = new Dictionary<string, AuthUser>();

        public AuthUser CurrentUser { get; private set; }
        public event Action<AuthUser> StateChanged;

        public Task Initialize() => Task.CompletedTask;

        public void RegisterGoogleCredential(string idToken, string uid, string displayName = null)
        {
            RequireToken(idToken);
            googleUsers[idToken] = new AuthUser(uid, false, displayName);
        }

        public Task<AuthUser> SignInAnonymously()
        {
            if (CurrentUser?.IsAnonymous == true)
                return Task.FromResult(CurrentUser);
            SetCurrent(new AuthUser("guest-" + Guid.NewGuid().ToString("N"), true));
            return Task.FromResult(CurrentUser);
        }

        public Task<AuthUser> SignInWithGoogle(string idToken)
        {
            RequireToken(idToken);
            if (!googleUsers.TryGetValue(idToken, out AuthUser user))
                throw new InvalidOperationException("The fake has no Google account registered for this token.");

            SetCurrent(user);
            return Task.FromResult(CurrentUser);
        }

        public Task<AuthUser> LinkGuestToGoogle(string idToken)
        {
            RequireToken(idToken);
            if (CurrentUser == null || !CurrentUser.IsAnonymous)
                throw new InvalidOperationException("An anonymous user is required before linking a Google account.");

            if (googleUsers.TryGetValue(idToken, out AuthUser existing) && existing.Uid != CurrentUser.Uid)
                throw new AuthLinkConflictException("This Google account is already linked to another user.");

            var linked = new AuthUser(CurrentUser.Uid, false, existing?.DisplayName);
            googleUsers[idToken] = linked;
            SetCurrent(linked);
            return Task.FromResult(CurrentUser);
        }

        public void SignOut() => SetCurrent(null);

        private static void RequireToken(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentException("A Google ID token is required.", nameof(idToken));
        }

        private void SetCurrent(AuthUser user)
        {
            if (Equals(CurrentUser, user))
                return;

            CurrentUser = user;
            StateChanged?.Invoke(user);
        }
    }
}
