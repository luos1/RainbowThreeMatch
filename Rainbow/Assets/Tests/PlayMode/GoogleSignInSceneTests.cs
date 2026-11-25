using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rainbow.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class GoogleSignInSceneTests
{
    [SetUp]
    public void SetUp()
    {
        var auth = new FakeAuthService();
        auth.RegisterGoogleCredential("test-token", "test-account");
        AccountSession.SetServices(auth, new InMemoryCloudSaveService());
    }

    [TearDown]
    public void ResetSession()
    {
        AccountSession.Reset();
        GoogleSignIn.Provider = new GoogleSignIn();
    }

    [UnityTest]
    public IEnumerator StartupPresentsGoogleSignInAndStaysOnTheLoginScene()
    {
        yield return SceneManager.LoadSceneAsync("00.Loading");
        yield return null;

        var signIn = FindButton("Sign in with Google");
        Assert.That(signIn, Is.Not.Null, "The first build scene must present the Google sign-in action.");
        Click(signIn);
        Assert.That(Status, Does.Contain("Android 또는 iOS 기기"));
        Assert.That(signIn.interactable, Is.True);
        Assert.That(AccountSession.Auth.CurrentUser, Is.Null);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("00.Loading"));
    }

    [UnityTest]
    public IEnumerator GoogleSuccessOpensLobbyAndThePlayButtonStartsGame()
    {
        var provider = new GoogleSignInDouble();
        GoogleSignIn.Provider = provider;
        yield return SceneManager.LoadSceneAsync("00.Loading");
        yield return null;

        var signIn = FindButton("Sign in with Google");
        Click(signIn);
        Object.FindObjectOfType<Login>().OnClickLogin();
        Assert.That(provider.Attempts, Is.EqualTo(1));
        Assert.That(signIn.interactable, Is.False);

        provider.Complete(SignInOutcome.Success, "test-token");
        yield return WaitForScene("01.Lobby");
        Assert.That(AccountSession.Auth.CurrentUser?.Uid, Is.EqualTo("test-account"));
        Click(FindButton("플레이"));
        yield return WaitForScene("02.Game");
        Assert.That(Object.FindObjectOfType<Game>(), Is.Not.Null);
        Assert.That(AccountSession.Auth.CurrentUser?.Uid, Is.EqualTo("test-account"));
    }

    [UnityTest]
    public IEnumerator CancellationFailureAndIncompleteSuccessAllowRetryWithoutOpeningLobby()
    {
        var provider = new GoogleSignInDouble();
        GoogleSignIn.Provider = provider;
        yield return SceneManager.LoadSceneAsync("00.Loading");
        yield return null;

        var signIn = FindButton("Sign in with Google");
        Click(signIn);
        provider.Complete(SignInOutcome.Canceled);
        Assert.That(Status, Does.Contain("취소"));
        Assert.That(signIn.interactable, Is.True);

        Click(signIn);
        provider.Complete(SignInOutcome.Failed);
        Assert.That(Status, Does.Contain("다시 시도"));
        Assert.That(signIn.interactable, Is.True);

        Click(signIn);
        provider.Complete(SignInOutcome.Success);
        Assert.That(Status, Does.Contain("다시 시도"));
        Assert.That(signIn.interactable, Is.True);
        Assert.That(AccountSession.Auth.CurrentUser, Is.Null);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("00.Loading"));
    }

    [UnityTest]
    public IEnumerator TimeoutClearsTheRequestAndIgnoresLateSuccess()
    {
        var provider = new GoogleSignInDouble();
        GoogleSignIn.Provider = provider;
        yield return SceneManager.LoadSceneAsync("00.Loading");
        yield return null;

        Object.FindObjectOfType<Login>().TimeoutSeconds = 0;
        var signIn = FindButton("Sign in with Google");
        Click(signIn);
        yield return null;
        yield return null;

        Assert.That(Status, Does.Contain("시간이 초과"));
        Assert.That(provider.Cleanups, Is.EqualTo(1));
        Assert.That(signIn.interactable, Is.True);
        provider.Complete(SignInOutcome.Success, "late-test-account");
        yield return null;
        Assert.That(AccountSession.Auth.CurrentUser, Is.Null);
        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("00.Loading"));
    }

    [UnityTest]
    public IEnumerator LobbyRequiresSessionAndSignOutReturnsToLogin()
    {
        var provider = new GoogleSignInDouble();
        GoogleSignIn.Provider = provider;
        yield return SceneManager.LoadSceneAsync("01.Lobby");
        yield return WaitForScene("00.Loading");

        Click(FindButton("Sign in with Google"));
        provider.Complete(SignInOutcome.Success, "test-token");
        yield return WaitForScene("01.Lobby");
        Click(FindButton("로그아웃"));
        yield return WaitForScene("00.Loading");

        Assert.That(AccountSession.Auth.CurrentUser, Is.Null);
        Assert.That(provider.SignedOut, Is.True);
        Assert.That(FindButton("Sign in with Google").interactable, Is.True);
    }

    private static string Status => GameObject.Find("Sign-in status").GetComponent<Text>().text;

    private static Button FindButton(string name) => Object.FindObjectsOfType<Button>()
        .SingleOrDefault(button => button.name == name);

    private static IEnumerator WaitForScene(string name)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + 10;
        while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;

        Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(name));
        yield return null;
    }

    private static void Click(Button button)
    {
        Assert.That(button, Is.Not.Null);
        Canvas.ForceUpdateCanvases();
        var pointer = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Assert.That(hits.FirstOrDefault().gameObject, Is.EqualTo(button.gameObject));
        ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }

    private sealed class GoogleSignInDouble : IGoogleSignIn
    {
        internal int Attempts;
        internal int Cleanups;
        internal bool SignedOut;
        private Action<SignInOutcome, string> onCompleted;

        public Action Begin(Action<SignInOutcome, string> completed)
        {
            Attempts++;
            onCompleted = completed;
            return () => Cleanups++;
        }

        public void SignOut() => SignedOut = true;

        internal void Complete(SignInOutcome outcome, string id = null) => onCompleted(outcome, id);
    }
}
