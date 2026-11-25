using System;
using Rainbow.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
    internal const string SceneName = "01.Lobby";
    private readonly GoogleSignInRequest request = new GoogleSignInRequest();
    private Button play;
    private Button link;
    private Button recover;
    private Button signOut;
    private Button retrySync;
    private Text status;
    private Text bestScore;
    private bool leaving;
    private bool linking;
    private bool syncing;
    private bool linkConflict;
    private bool confirmGuestSignOut;
    private IAuthService auth;
    private int syncAttempt;

    private void Start()
    {
        auth = AccountSession.Auth;
        if (auth.CurrentUser == null)
        {
            ReturnToLogin();
            return;
        }

        var canvas = new GameObject("Lobby Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.layer = LayerMask.NameToLayer("UI");
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        ScreenControls.Label(canvas.transform, "Rainbow", 355, 82);
        bestScore = ScreenControls.Label(canvas.transform, "", 245, 40);
        play = ScreenControls.Button(canvas.transform, "플레이", 105, StartGame);
        link = ScreenControls.Button(canvas.transform, "Google 계정 연결", -35, () => BeginLink(false));
        recover = ScreenControls.Button(canvas.transform, "기존 Google 계정에 합치기", -175, () => BeginLink(true));
        signOut = ScreenControls.Button(canvas.transform, "로그아웃", -315, SignOut);
        retrySync = ScreenControls.Button(canvas.transform, "저장 동기화 재시도", -455, SyncProgress);
        retrySync.gameObject.SetActive(false);
        status = ScreenControls.Label(canvas.transform, "", -595, 33);
        status.name = "Account status";
        auth.StateChanged += OnAuthChanged;
        RefreshControls();
        SyncProgress();
    }

    private void Update() => request.Tick();
    private void OnDisable() => request.Cancel();
    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthChanged;
    }

    private void OnAuthChanged(AuthUser user)
    {
        if (user == null)
            ReturnToLogin();
        else
        {
            RefreshControls();
            if (!linking)
                SyncProgress();
        }
    }

    private void StartGame()
    {
        if (leaving || linking || AccountSession.Auth.CurrentUser == null)
            return;

        leaving = true;
        SceneManager.LoadSceneAsync("02.Game");
    }

    private void BeginLink(bool recoverExisting)
    {
        if (leaving || linking || AccountSession.Auth.CurrentUser?.IsAnonymous != true)
            return;

        linking = true;
        confirmGuestSignOut = false;
        RefreshControls();
        status.text = "Google 계정 선택 대기 중…";
        request.Begin(120, (outcome, token) => CompleteLink(outcome, token, recoverExisting));
    }

    private async void CompleteLink(SignInOutcome outcome, string token, bool recoverExisting)
    {
        try
        {
            if (outcome != SignInOutcome.Success || string.IsNullOrWhiteSpace(token))
            {
                status.text = GoogleSignInRequest.Message(outcome);
                return;
            }

            AuthUser guest = AccountSession.Auth.CurrentUser;
            SaveData guestSave = await AccountSession.Progress.LoadLocal(guest.Uid);
            AuthUser linked;
            try
            {
                linked = await AccountSession.Auth.LinkGuestToGoogle(token);
            }
            catch (AuthLinkConflictException) when (recoverExisting)
            {
                guestSave = await AccountSession.Progress.Sync(guest.Uid);
                linked = await AccountSession.Auth.SignInWithGoogle(token);
            }

            await AccountSession.Progress.KeepLinkedProgress(linked, guestSave);
            if (!this)
                return;

            linkConflict = false;
            status.text = "Google 계정에 연결했습니다.";
            SyncProgress();
        }
        catch (AuthLinkConflictException)
        {
            if (!this)
                return;
            linkConflict = true;
            status.text = "이미 연결된 Google 계정입니다. 게스트 기록은 유지되며, 합치기를 직접 선택할 수 있습니다.";
        }
        catch (NotSupportedException)
        {
            if (this)
                status.text = "이 빌드에 Firebase Google 계정 연결 설정이 없습니다.";
        }
        catch (Exception)
        {
            if (this)
                status.text = "계정 연결 또는 기록 합치기에 실패했습니다. 다시 시도해 주세요.";
        }
        finally
        {
            if (this)
            {
                linking = false;
                RefreshControls();
            }
        }
    }

    private async void SyncProgress()
    {
        string uid = AccountSession.Auth.CurrentUser?.Uid;
        if (leaving || uid == null)
            return;

        int active = ++syncAttempt;
        syncing = true;
        retrySync.gameObject.SetActive(false);
        bestScore.text = "최고 점수 불러오는 중…";
        try
        {
            ShowBestScore(active, uid, await AccountSession.Progress.LoadLocal(uid));
            ShowBestScore(active, uid, await AccountSession.Progress.Sync(uid));
        }
        catch (Exception)
        {
            if (IsCurrentSync(active, uid))
                retrySync.gameObject.SetActive(true);
        }
        finally
        {
            if (IsCurrentSync(active, uid))
            {
                syncing = false;
                RefreshControls();
            }
        }
    }

    private bool IsCurrentSync(int active, string uid) => this && !leaving
        && syncAttempt == active && auth.CurrentUser?.Uid == uid;

    private void ShowBestScore(int active, string uid, SaveData save)
    {
        if (IsCurrentSync(active, uid))
            bestScore.text = $"최고 점수  {(save?.HighScore ?? 0):N0}";
    }

    private void RefreshControls()
    {
        bool isGuest = AccountSession.Auth.CurrentUser?.IsAnonymous == true;
        link.gameObject.SetActive(isGuest);
        recover.gameObject.SetActive(isGuest && linkConflict);
        play.interactable = !linking;
        link.interactable = !linking;
        recover.interactable = !linking;
        signOut.interactable = !linking;
        retrySync.interactable = !syncing && !linking;
    }

    private void SignOut()
    {
        if (leaving || linking)
            return;

        if (AccountSession.Auth.CurrentUser?.IsAnonymous == true && !confirmGuestSignOut)
        {
            confirmGuestSignOut = true;
            status.text = "계정을 연결하지 않은 게스트 기록은 복구하지 못할 수 있습니다. 로그아웃을 다시 누르면 계속합니다.";
            return;
        }

        try
        {
            AccountSession.Auth.SignOut();
        }
        catch (Exception)
        {
            status.text = "로그아웃에 실패했습니다. 다시 시도해 주세요.";
            return;
        }

        try
        {
            GoogleSignIn.Provider.SignOut();
        }
        catch (Exception)
        {
            Debug.LogWarning("Google SDK sign-out failed. The Rainbow session was cleared.");
        }

        ReturnToLogin();
    }

    private void ReturnToLogin()
    {
        if (leaving)
            return;

        leaving = true;
        SceneManager.LoadSceneAsync(Loading.SceneName);
    }
}
