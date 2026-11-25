using System;
using UnityEngine;
using UnityEngine.UI;

public class Login : MonoBehaviour
{
    private Button signIn;
    private Text status;
    private Action stopWaiting;
    private bool pending;
    private int attempt;
    private double deadline;

    internal event Action SignedIn;
    internal double TimeoutSeconds { get; set; } = 120;

    private void Awake()
    {
        ScreenControls.Label(transform, "Rainbow", 144, 82);
        signIn = ScreenControls.GoogleButton(transform, OnClickLogin);
        status = ScreenControls.Label(transform, "Sign-in status", -170, 32);
        status.text = "";
    }

    public void OnClickLogin()
    {
        if (pending || UserData.Current != null || !isActiveAndEnabled)
            return;

        pending = true;
        int activeAttempt = ++attempt;
        deadline = Time.realtimeSinceStartupAsDouble + TimeoutSeconds;
        signIn.interactable = false;
        status.text = "Waiting for Google…";

        try
        {
            Action cancel = GoogleSignIn.Provider.Begin((outcome, id) => Complete(activeAttempt, outcome, id));
            if (pending && attempt == activeAttempt)
                stopWaiting = cancel;
            else
                cancel?.Invoke();
        }
        catch (Exception)
        {
            Complete(activeAttempt, SignInOutcome.Failed, null);
        }
    }

    private void Update()
    {
        if (pending && (!Application.isMobilePlatform || Application.isFocused) && Time.realtimeSinceStartupAsDouble >= deadline)
            Complete(attempt, SignInOutcome.TimedOut, null);
    }

    private void Complete(int activeAttempt, SignInOutcome outcome, string id)
    {
        if (!pending || activeAttempt != attempt)
            return;

        StopWaiting();
        if (outcome == SignInOutcome.Success && !string.IsNullOrWhiteSpace(id))
        {
            UserData.StartSession(id);
            SignedIn?.Invoke();
            return;
        }

        signIn.interactable = true;
        switch (outcome)
        {
            case SignInOutcome.Canceled:
                status.text = "Sign-in canceled. You can try again.";
                break;
            case SignInOutcome.NotConfigured:
                status.text = "Google sign-in is not configured for this build.";
                break;
            case SignInOutcome.Unsupported:
                status.text = "Google sign-in requires an Android or iOS device.";
                break;
            case SignInOutcome.TimedOut:
                status.text = "Sign-in timed out. Please try again.";
                break;
            default:
                status.text = "Couldn't sign in. Please try again.";
                break;
        }
    }

    private void StopWaiting()
    {
        pending = false;
        Action cancel = stopWaiting;
        stopWaiting = null;
        cancel?.Invoke();
    }

    private void OnDisable()
    {
        if (!pending)
            return;

        StopWaiting();
        signIn.interactable = true;
        status.text = "";
    }
}
