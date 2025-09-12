using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] RoomPlacer placer;
    [SerializeField] QuotaMeta defaultMeta;
    [SerializeField] ThemeId currentThemeId = ThemeId.Dungeon;  //�⺻ �׸�

    public ThemeId CurrentThemeId => currentThemeId;
    public void SetTheme(ThemeId theme) { currentThemeId = theme; }  //�׸� ����

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartRun();
    }

    //�� ����
    public void StartRun()
    {
        defaultMeta = new QuotaMeta
        {
            //combatRooms = new QuotaRange { min = 5, max = 5 },
            //eliteRooms = new QuotaRange { min = 2, max = 2 },
            //bossRooms = 1,
            //shopRooms = new QuotaRange { min = 1, max = 1 },
            //eventRooms = new QuotaRange { min = 1, max = 2 },

            combatRooms = new QuotaRange { min = 20, max = 20 },
            eliteRooms = new QuotaRange { min = 2, max = 2 },
            bossRooms = 1,
            shopRooms = new QuotaRange { min = 2, max = 3 },
            eventRooms = new QuotaRange { min = 3, max = 4 },
        };

        if (!placer)
        {
            placer = FindObjectOfType<RoomPlacer>();
            if (!placer) { Debug.Log("[GameManager] RoomPlacer ����"); return; }
        }

        placer.GenerateWithQuota(defaultMeta);
    }

    //�� ����ŸƮ
    public void RestartRun()
    {
        // ===== TODO: ĳ���� ��Ȱ �����ϱ� =====
    }
}