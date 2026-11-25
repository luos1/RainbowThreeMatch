using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class GoogleSignInSceneTests
{
    [TearDown]
    public void ResetSession()
    {
        UserData.ClearSession();
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
        Assert.That(Status, Does.Contain("requires an Android or iOS device"));
        Assert.That(signIn.interactable, Is.True);
        Assert.That(UserData.Current, Is.Null);
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

        provider.Complete(SignInOutcome.Success, "test-account");
        yield return WaitForScene("01.Lobby");
        Assert.That(UserData.Current?.ID, Is.EqualTo("test-account"));
        Click(FindButton("Play"));
        yield return WaitForScene("02.Game");
        Assert.That(Object.FindObjectOfType<Game>(), Is.Not.Null);
        Assert.That(UserData.Current?.ID, Is.EqualTo("test-account"));
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
        Assert.That(Status, Does.Contain("canceled"));
        Assert.That(signIn.interactable, Is.True);

        Click(signIn);
        provider.Complete(SignInOutcome.Failed);
        Assert.That(Status, Does.Contain("try again"));
        Assert.That(signIn.interactable, Is.True);

        Click(signIn);
        provider.Complete(SignInOutcome.Success);
        Assert.That(Status, Does.Contain("try again"));
        Assert.That(signIn.interactable, Is.True);
        Assert.That(UserData.Current, Is.Null);
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

        Assert.That(Status, Does.Contain("timed out"));
        Assert.That(provider.Cleanups, Is.EqualTo(1));
        Assert.That(signIn.interactable, Is.True);
        provider.Complete(SignInOutcome.Success, "late-test-account");
        yield return null;
        Assert.That(UserData.Current, Is.Null);
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
        provider.Complete(SignInOutcome.Success, "test-account");
        yield return WaitForScene("01.Lobby");
        Click(FindButton("Sign out"));
        yield return WaitForScene("00.Loading");

        Assert.That(UserData.Current, Is.Null);
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
