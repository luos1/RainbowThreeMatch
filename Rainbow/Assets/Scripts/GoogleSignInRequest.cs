using System;
using UnityEngine;

internal sealed class GoogleSignInRequest
{
    private Action<SignInOutcome, string> complete;
    private Action cancel;
    private double deadline;
    private int attempt;

    internal bool IsWaiting => complete != null;

    internal void Begin(double timeout, Action<SignInOutcome, string> completed)
    {
        Cancel();
        complete = completed;
        int active = ++attempt;
        deadline = Time.realtimeSinceStartupAsDouble + timeout;

        try
        {
            Action cleanup = GoogleSignIn.Provider.Begin((outcome, token) => Finish(active, outcome, token));
            if (active == attempt && complete != null)
                cancel = cleanup;
            else
                cleanup?.Invoke();
        }
        catch (Exception)
        {
            Finish(active, SignInOutcome.Failed, null);
        }
    }

    internal void Tick()
    {
        if (complete != null && (!Application.isMobilePlatform || Application.isFocused)
            && Time.realtimeSinceStartupAsDouble >= deadline)
            Finish(attempt, SignInOutcome.TimedOut, null);
    }

    internal void Cancel()
    {
        complete = null;
        Action cleanup = cancel;
        cancel = null;
        cleanup?.Invoke();
    }

    private void Finish(int active, SignInOutcome outcome, string token)
    {
        if (active != attempt || complete == null)
            return;

        var handler = complete;
        Cancel();
        handler(outcome, token);
    }

    internal static string Message(SignInOutcome outcome)
    {
        switch (outcome)
        {
            case SignInOutcome.Canceled: return "Google 로그인을 취소했습니다.";
            case SignInOutcome.NotConfigured: return "이 빌드에 Google 로그인 설정이 없습니다.";
            case SignInOutcome.Unsupported: return "Google 로그인은 Android 또는 iOS 기기에서 사용할 수 있습니다.";
            case SignInOutcome.TimedOut: return "Google 로그인 시간이 초과됐습니다. 다시 시도해 주세요.";
            default: return "Google 로그인에 실패했습니다. 다시 시도해 주세요.";
        }
    }
}
