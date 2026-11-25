using System;
using Rainbow.Services;
using UnityEngine;
using UnityEngine.UI;

public class Login : MonoBehaviour
{
    private readonly GoogleSignInRequest request = new GoogleSignInRequest();
    private Button signIn;
    private Button guest;
    private Text status;
    private bool pending;

    internal event Action SignedIn;
    internal double TimeoutSeconds { get; set; } = 120;

    private void Awake()
    {
        ScreenControls.Label(transform, "Rainbow", 185, 82);
        signIn = ScreenControls.GoogleButton(transform, OnClickLogin);
        guest = ScreenControls.Button(transform, "게스트로 시작", -155, OnClickGuest);
        status = ScreenControls.Label(transform, "Sign-in status", -285, 32);
        status.text = "";
        SetPending(true);
    }

    private async void Start()
    {
        try
        {
            await AccountSession.Auth.Initialize();
            if (this && isActiveAndEnabled && AccountSession.Auth.CurrentUser != null)
                SignedIn?.Invoke();
        }
        catch (Exception)
        {
            if (this)
                status.text = "계정 서비스를 불러오지 못했습니다. 다시 시도해 주세요.";
        }
        finally
        {
            if (this)
                SetPending(false);
        }
    }

    public void OnClickLogin()
    {
        if (!CanStart())
            return;

        SetPending(true);
        status.text = "Google 로그인 대기 중…";
        request.Begin(TimeoutSeconds, CompleteGoogle);
    }

    private async void OnClickGuest()
    {
        if (!CanStart())
            return;

        SetPending(true);
        status.text = "게스트 계정에 로그인하는 중…";
        try
        {
            await AccountSession.Auth.Initialize();
            await AccountSession.Auth.SignInAnonymously();
            NotifySignedIn();
        }
        catch (Exception)
        {
            if (this)
                status.text = "게스트 로그인에 실패했습니다. 다시 시도해 주세요.";
        }
        finally
        {
            if (this)
                SetPending(false);
        }
    }

    private void Update() => request.Tick();
    private void OnDisable()
    {
        if (request.IsWaiting)
        {
            request.Cancel();
            SetPending(false);
        }
    }

    private async void CompleteGoogle(SignInOutcome outcome, string token)
    {
        try
        {
            if (outcome != SignInOutcome.Success || string.IsNullOrWhiteSpace(token))
            {
                status.text = GoogleSignInRequest.Message(outcome);
                return;
            }

            await AccountSession.Auth.Initialize();
            await AccountSession.Auth.SignInWithGoogle(token);
            NotifySignedIn();
        }
        catch (NotSupportedException)
        {
            if (this)
                status.text = "이 빌드에 Firebase Google 로그인 설정이 없습니다.";
        }
        catch (Exception)
        {
            if (this)
                status.text = GoogleSignInRequest.Message(SignInOutcome.Failed);
        }
        finally
        {
            if (this)
                SetPending(false);
        }
    }

    private bool CanStart() => !pending && AccountSession.Auth.CurrentUser == null && isActiveAndEnabled;

    private void NotifySignedIn()
    {
        if (this && isActiveAndEnabled && AccountSession.Auth.CurrentUser != null)
            SignedIn?.Invoke();
    }

    private void SetPending(bool value)
    {
        pending = value;
        signIn.interactable = !value;
        guest.interactable = !value;
    }
}
