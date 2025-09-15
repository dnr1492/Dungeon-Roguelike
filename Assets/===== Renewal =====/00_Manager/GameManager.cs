using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] RoomPlacer placer;
    [SerializeField] QuotaMeta defaultMeta;
    [SerializeField] ThemeId currentThemeId = ThemeId.Dungeon;  //기본 테마

    public ThemeId CurrentThemeId => currentThemeId;
    public void SetTheme(ThemeId theme) { currentThemeId = theme; }  //테마 변경

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateBulletRoot();
    }

    private void Start()
    {
        StartRun();
    }

    //런 시작
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
            if (!placer) { Debug.Log("[GameManager] RoomPlacer 없음"); return; }
        }

        placer.GenerateWithQuota(defaultMeta);
    }

    //런 리스타트
    public void RestartRun()
    {
        // ===== TODO: 캐릭터 부활 구현하기 =====
    }

    #region 총알 풀링
    private Transform bulletsRoot;
    private readonly Dictionary<Bullet, Queue<Bullet>> pools = new();
    private readonly Dictionary<Bullet, Bullet> instanceToPrefab = new();

    //총알 풀링용 Root 생성
    private void CreateBulletRoot()
    {
        if (!bulletsRoot)
        {
            var root = new GameObject("Pool_Bullets");
            bulletsRoot = root.transform;
            bulletsRoot.SetParent(transform, false);
        }
    }

    //총알을 풀링에서 가져오기
    public Bullet GetBullet(Bullet prefab)
    {
        if (!prefab) return null;
        if (!pools.TryGetValue(prefab, out var q) || q.Count == 0)
        {
            //부족할 경우 새로 생성
            var b = Instantiate(prefab);
            instanceToPrefab[b] = prefab;
            return b;
        }
        var inst = q.Dequeue();
        if (inst) inst.transform.SetParent(null, true);  //월드로 꺼냄
        return inst;
    }

    //총알을 풀링에 반환
    public void ReturnBullet(Bullet b)
    {
        if (!b) return;
        if (!instanceToPrefab.TryGetValue(b, out var prefab) || !prefab)
        {
            Destroy(b.gameObject);
            return;
        }
        if (!pools.TryGetValue(prefab, out var q))
        {
            q = new Queue<Bullet>();
            pools[prefab] = q;
        }
        b.gameObject.SetActive(false);
        b.transform.SetParent(bulletsRoot, false);
        q.Enqueue(b);
    }
    #endregion
}