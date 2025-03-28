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
    /// �ȵ���̵� �� ����
    /// </summary>
    void QuitAndroid()
    {
        if (Application.platform != RuntimePlatform.Android) return;
        Application.Quit();
    }

    /// <summary>
    /// �� ��ȯ
    /// </summary>
    /// <param name="changeSceneName"></param>
    void ChangeScene(string changeSceneName)
    {
        SceneManager.LoadSceneAsync(changeSceneName);
    }
}
