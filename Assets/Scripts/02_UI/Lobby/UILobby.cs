using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UILobby : MonoBehaviour
{
    [SerializeField] Button btnStart, btnQuit;

    protected virtual void Init()
    {

    }

    protected virtual void Awake()
    {
        if (btnStart != null) btnStart.onClick.AddListener(() => {
            ChangeScene("PlayScene");
        });
        if (btnQuit != null) btnQuit.onClick.AddListener(() => {
            QuitAndroid();
        });
    }

    /// <summary>
    /// 안드로이드 앱 종료
    /// </summary>
    void QuitAndroid()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        Application.Quit();
    }

    /// <summary>
    /// 씬 전환
    /// </summary>
    /// <param name="changeSceneName"></param>
    void ChangeScene(string changeSceneName)
    {
        SceneManager.LoadSceneAsync(changeSceneName);
    }
}
