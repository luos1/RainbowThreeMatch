using UnityEngine;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Login login;

    internal const string SceneName = "00.Loading";
    private bool leaving;

    private void OnEnable() => login.SignedIn += OpenLobby;
    private void OnDisable() => login.SignedIn -= OpenLobby;

    private void OpenLobby()
    {
        if (leaving)
            return;

        leaving = true;
        login.gameObject.SetActive(false);
        loadingPanel.SetActive(true);
        SceneManager.LoadSceneAsync(Lobby.SceneName);
    }
}
