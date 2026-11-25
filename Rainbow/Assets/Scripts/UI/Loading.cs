using UnityEngine;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Login login;

    internal const string SceneName = "00.Loading";

    private void OnEnable() => login.SignedIn += OpenLobby;
    private void OnDisable() => login.SignedIn -= OpenLobby;

    private void Start()
    {
        if (UserData.Current != null)
            OpenLobby();
    }

    private void OpenLobby()
    {
        login.gameObject.SetActive(false);
        loadingPanel.SetActive(true);
        SceneManager.LoadSceneAsync(Lobby.SceneName);
    }
}
