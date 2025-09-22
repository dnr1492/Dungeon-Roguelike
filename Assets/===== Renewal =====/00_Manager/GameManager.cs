using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] RoomPlacer placer;
    [SerializeField] RunConfig run;

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
    }

    private void Start()
    {
        StartRun();
    }

    //�� ����
    public void StartRun()
    {
        if (!placer)
        {
            placer = FindObjectOfType<RoomPlacer>();
            if (!placer) { Debug.Log("[GameManager] RoomPlacer ����"); return; }
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

    //�� ����ŸƮ
    public void RestartRun()
    {
        // ===== TODO: ĳ���� ��Ȱ �����ϱ� =====
    }

    #region �Ѿ� Ǯ��
    private Transform bulletsRoot;
    private readonly Dictionary<Bullet, Queue<Bullet>> pools = new();
    private readonly Dictionary<Bullet, Bullet> instanceToPrefab = new();

    //�Ѿ� Ǯ���� Root ����
    private void CreateBulletRoot()
    {
        if (!bulletsRoot)
        {
            var root = new GameObject("Pool_Bullets");
            bulletsRoot = root.transform;
            bulletsRoot.SetParent(transform, false);
        }
    }

    //�Ѿ��� Ǯ������ ��������
    public Bullet GetBullet(Bullet prefab)
    {
        if (!prefab) return null;
        if (!pools.TryGetValue(prefab, out var q) || q.Count == 0)
        {
            //������ ��� ���� ����
            var b = Instantiate(prefab);
            instanceToPrefab[b] = prefab;
            return b;
        }
        var inst = q.Dequeue();
        if (inst) inst.transform.SetParent(null, true);  //����� ����
        return inst;
    }

    //�Ѿ��� Ǯ���� ��ȯ
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