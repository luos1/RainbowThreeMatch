using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
    internal const string SceneName = "01.Lobby";
    private bool leaving;

    private void Start()
    {
        if (UserData.Current == null)
        {
            SceneManager.LoadSceneAsync(Loading.SceneName);
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

        ScreenControls.Label(canvas.transform, "Rainbow", 200, 82);
        ScreenControls.Button(canvas.transform, "Play", 0, StartGame);
        ScreenControls.Button(canvas.transform, "Sign out", -160, SignOut);
    }

    private void StartGame()
    {
        if (leaving || UserData.Current == null)
            return;

        leaving = true;
        SceneManager.LoadSceneAsync("02.Game");
    }

    private void SignOut()
    {
        if (leaving)
            return;

        leaving = true;
        UserData.ClearSession();
        try
        {
            GoogleSignIn.Provider.SignOut();
        }
        catch (Exception)
        {
            Debug.LogWarning("Google SDK sign-out failed. The Rainbow session was cleared.");
        }

        SceneManager.LoadSceneAsync(Loading.SceneName);
    }
}
