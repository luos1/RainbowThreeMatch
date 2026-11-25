using System;
using UnityEngine;
using Universal.UniversalSDK;

internal enum SignInOutcome
{
    Success,
    Canceled,
    Failed,
    NotConfigured,
    Unsupported,
    TimedOut
}

internal interface IGoogleSignIn
{
    Action Begin(Action<SignInOutcome, string> completed);
    void SignOut();
}

internal sealed class GoogleSignIn : IGoogleSignIn
{
    internal static IGoogleSignIn Provider { get; set; } = new GoogleSignIn();

    public Action Begin(Action<SignInOutcome, string> completed)
    {
        if (!Application.isMobilePlatform || Application.isEditor)
        {
            completed(SignInOutcome.Unsupported, null);
            return null;
        }

        if (!NativeInterface.HasGoogleConfiguration())
        {
            completed(SignInOutcome.NotConfigured, null);
            return null;
        }

        return UniversalSDK.Ins.BeginLogin(LoginType.GOOGLE, result => Complete(result, completed));
    }

    internal static void Complete(Result<LoginResult> result, Action<SignInOutcome, string> completed)
    {
        result.Match(
            profile =>
            {
                bool valid = profile != null && !string.IsNullOrWhiteSpace(profile.UserID)
                    && !string.IsNullOrWhiteSpace(profile.IdToken);
                completed(valid ? SignInOutcome.Success : SignInOutcome.Failed, valid ? profile.UserID : null);
            },
            error => completed(error?.Code == (int)ErrorCode.CANCEL
                ? SignInOutcome.Canceled : SignInOutcome.Failed, null));
    }

    public void SignOut()
    {
        if (Application.isMobilePlatform && !Application.isEditor)
            UniversalSDK.Ins.Logout();
    }
}
