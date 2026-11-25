using System;
using System.Threading.Tasks;

namespace Rainbow.Services
{
    public sealed class AuthUser : IEquatable<AuthUser>
    {
        public string Uid { get; }
        public bool IsAnonymous { get; }
        public string DisplayName { get; }

        public AuthUser(string uid, bool isAnonymous, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(uid))
                throw new ArgumentException("A user ID is required.", nameof(uid));

            Uid = uid;
            IsAnonymous = isAnonymous;
            DisplayName = displayName;
        }

        public bool Equals(AuthUser other) => other != null
            && Uid == other.Uid
            && IsAnonymous == other.IsAnonymous
            && DisplayName == other.DisplayName;

        public override bool Equals(object obj) => Equals(obj as AuthUser);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Uid.GetHashCode();
                hash = (hash * 397) ^ IsAnonymous.GetHashCode();
                return (hash * 397) ^ (DisplayName == null ? 0 : DisplayName.GetHashCode());
            }
        }
    }

    public sealed class AuthLinkConflictException : Exception
    {
        public AuthLinkConflictException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }

    public interface IAuthService
    {
        AuthUser CurrentUser { get; }
        event Action<AuthUser> StateChanged;

        Task Initialize();
        Task<AuthUser> SignInAnonymously();
        Task<AuthUser> SignInWithGoogle(string idToken);
        Task<AuthUser> LinkGuestToGoogle(string idToken);
        void SignOut();
    }
}
