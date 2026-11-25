using System;
using System.Threading;
using System.Threading.Tasks;
#if RAINBOW_FIREBASE
using Firebase;
using Firebase.Auth;
#endif

namespace Rainbow.Services
{
    public sealed class FirebaseAuthService : IAuthService, IDisposable
    {
        private readonly SemaphoreSlim operations = new SemaphoreSlim(1, 1);
        private int sessionVersion;
#if RAINBOW_FIREBASE
        private FirebaseAuth auth;
#endif

        public AuthUser CurrentUser { get; private set; }
        public event Action<AuthUser> StateChanged;

        public async Task Initialize()
        {
#if RAINBOW_FIREBASE
            if (auth != null)
                return;
            await FirebaseRuntime.EnsureReady();
            auth = FirebaseAuth.DefaultInstance;
            auth.StateChanged -= OnFirebaseStateChanged;
            auth.StateChanged += OnFirebaseStateChanged;
            SetCurrent(ToAuthUser(auth.CurrentUser));
#else
            await FirebaseUnavailable();
#endif
        }

        public Task<AuthUser> SignInAnonymously() => Run(async () =>
        {
#if RAINBOW_FIREBASE
            AuthResult result = await RequireAuth().SignInAnonymouslyAsync();
            return ToAuthUser(result.User);
#else
            return await FirebaseUnavailable<AuthUser>();
#endif
        });

        public Task<AuthUser> SignInWithGoogle(string idToken) => Run(async () =>
        {
            RequireToken(idToken);
#if RAINBOW_FIREBASE
            using (Credential credential = GoogleAuthProvider.GetCredential(idToken, null))
                return ToAuthUser(await RequireAuth().SignInWithCredentialAsync(credential));
#else
            return await FirebaseUnavailable<AuthUser>();
#endif
        });

        public Task<AuthUser> LinkGuestToGoogle(string idToken) => Run(async () =>
        {
            RequireToken(idToken);
#if RAINBOW_FIREBASE
            FirebaseUser guest = RequireAuth().CurrentUser;
            if (guest == null || !guest.IsAnonymous)
                throw new InvalidOperationException("An anonymous user is required before linking a Google account.");

            try
            {
                using (Credential credential = GoogleAuthProvider.GetCredential(idToken, null))
                {
                    AuthResult result = await guest.LinkWithCredentialAsync(credential);
                    return ToAuthUser(result.User);
                }
            }
            catch (FirebaseException exception) when (
                exception.ErrorCode == (int)AuthError.CredentialAlreadyInUse)
            {
                throw new AuthLinkConflictException(
                    "This Google account is already linked. Guest data was kept on this device.", exception);
            }
#else
            return await FirebaseUnavailable<AuthUser>();
#endif
        });

        public void SignOut()
        {
            Interlocked.Increment(ref sessionVersion);
#if RAINBOW_FIREBASE
            auth?.SignOut();
#endif
            SetCurrent(null);
        }

        public void Dispose()
        {
#if RAINBOW_FIREBASE
            if (auth != null)
                auth.StateChanged -= OnFirebaseStateChanged;
#endif
        }

        private async Task<AuthUser> Run(Func<Task<AuthUser>> operation)
        {
            int operationVersion = sessionVersion;
            await operations.WaitAsync();
            try
            {
                if (operationVersion != sessionVersion)
                    throw new OperationCanceledException("The authentication session changed before the request started.");
                AuthUser user = await operation();
                if (operationVersion != sessionVersion)
                {
#if RAINBOW_FIREBASE
                    auth?.SignOut();
#endif
                    throw new OperationCanceledException("The authentication session changed while the request was running.");
                }

                SetCurrent(user);
                return user;
            }
            finally
            {
#if RAINBOW_FIREBASE
                if (auth != null && operationVersion == sessionVersion)
                    SetCurrent(ToAuthUser(auth.CurrentUser));
#endif
                operations.Release();
            }
        }

        private static void RequireToken(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
                throw new ArgumentException("A Google ID token is required.", nameof(idToken));
        }

#if RAINBOW_FIREBASE
        private FirebaseAuth RequireAuth() => auth ?? throw new InvalidOperationException(
            "Initialize the Firebase authentication service before signing in.");

        private void OnFirebaseStateChanged(object sender, EventArgs eventArgs)
        {
            if (operations.CurrentCount != 0)
                SetCurrent(ToAuthUser(auth.CurrentUser));
        }

        private static AuthUser ToAuthUser(FirebaseUser user) => user == null
            ? null
            : new AuthUser(user.UserId, user.IsAnonymous, user.DisplayName);
#endif

        private void SetCurrent(AuthUser user)
        {
            if (Equals(CurrentUser, user))
                return;

            CurrentUser = user;
            StateChanged?.Invoke(user);
        }

        private static Task FirebaseUnavailable() => Task.FromException(new NotSupportedException(
            "Firebase authentication requires the Firebase packages and the RAINBOW_FIREBASE compile define."));

        private static Task<T> FirebaseUnavailable<T>() => Task.FromException<T>(new NotSupportedException(
            "Firebase authentication requires the Firebase packages and the RAINBOW_FIREBASE compile define."));
    }
}
