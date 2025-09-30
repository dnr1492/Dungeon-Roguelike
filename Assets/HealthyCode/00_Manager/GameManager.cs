using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] Camera playerCam;
    [SerializeField] RectTransform uiDamageFloatingTextParent;
    [SerializeField] RoomPlacer placer;
    [SerializeField] RunConfig run;

    public Camera PlayerCam => playerCam;
    public RectTransform UIDamageFloatingTextParent => uiDamageFloatingTextParent;
    public ThemeId CurrentThemeId => run.themeId;
    public RunConfig CurrentRun => run;
    public void SetRun(RunConfig cfg) { run = cfg; }

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
        CreateDamageFloatingTextRoot();
    }

    private void Start()
    {
        StartRun();
    }

    //런 시작
    public void StartRun()
    {
        if (!placer)
        {
            placer = FindObjectOfType<RoomPlacer>();
            if (!placer) { Debug.Log("[GameManager] RoomPlacer 없음"); return; }
        }

        static QuotaRange toQR(RunConfig.QuotaRange r) => new QuotaRange { min = r.min, max = r.max };
        var meta = new QuotaMeta
        {
            combatRooms = toQR(run.meta.quotaCombat),
            eliteRooms = toQR(run.meta.quotaElite),
            bossRooms = run.meta.quotaBoss,
            shopRooms = toQR(run.meta.quotaShop),
            eventRooms = toQR(run.meta.quotaEvent),
        };

        placer.GenerateWithQuota(meta);
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

    #region 데미지 플로팅 텍스트 풀링
    private Transform damageFloatingTextRoot;
    private readonly Dictionary<UIDamageFloatingText, Queue<UIDamageFloatingText>> damageFloatingTextPools = new();
    private readonly Dictionary<UIDamageFloatingText, UIDamageFloatingText> damageFloatingTextInstanceToPrefab = new();

    private void CreateDamageFloatingTextRoot()
    {
        if (!damageFloatingTextRoot)
        {
            var root = new GameObject("Pool_DamageFloatingText");
            damageFloatingTextRoot = root.transform;
            damageFloatingTextRoot.SetParent(transform, false);
        }
    }

    public UIDamageFloatingText GetDamageFloatingText(UIDamageFloatingText prefab)
    {
        if (!prefab) return null;
        if (!damageFloatingTextPools.TryGetValue(prefab, out var q) || q.Count == 0)
        {
            var inst = Instantiate(prefab);
            damageFloatingTextInstanceToPrefab[inst] = prefab;
            return inst;
        }
        var p = q.Dequeue();
        if (p) p.transform.SetParent(null, true);
        return p;
    }

    public void ReturnDamageFloatingText(UIDamageFloatingText inst)
    {
        if (!inst) return;
        if (!damageFloatingTextInstanceToPrefab.TryGetValue(inst, out var prefab) || !prefab)
        {
            Destroy(inst.gameObject);
            return;
        }
        if (!damageFloatingTextPools.TryGetValue(prefab, out var q))
        {
            q = new Queue<UIDamageFloatingText>();
            damageFloatingTextPools[prefab] = q;
        }
        inst.gameObject.SetActive(false);
        inst.transform.SetParent(damageFloatingTextRoot, false);
        q.Enqueue(inst);
    }
    #endregion
}