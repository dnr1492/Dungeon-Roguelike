using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomEncounter : MonoBehaviour
{
    [Header("Enemy Pools")]
    [SerializeField] List<GameObject> enemyPoolCombat;
    [SerializeField] List<GameObject> enemyPoolElite;

    #region 적 생성 설정값
    private readonly Vector2Int enemyCountRangeCombat = new Vector2Int(10, 20);
    private readonly Vector2Int enemyCountRangeElite = new Vector2Int(20, 40);
    private readonly int minEnemyDistanceFromDoor = 2;
    private readonly int minEnemyDistanceFromWall = 1;
    private readonly int minEnemySpacing = 2;
    #endregion

    private PlacedRoom myRoom;
    private Grid dungeonGrid;
    private readonly List<Health> enemies = new();
    private int aliveEnemyCount = 0;
    private bool cleared = false;
    private Bounds roomBounds;

    //랜덤 스폰
    public void SeedEnemies(PlacedRoom pr, Grid grid)
    {
        myRoom = pr;
        dungeonGrid = grid;

        if (myRoom == null || myRoom.go == null || dungeonGrid == null) return;

        bool isCombat = myRoom.go.CompareTag(ConstClass.Tags.CombatRoom);
        bool isElite = myRoom.go.CompareTag(ConstClass.Tags.EliteRoom);
        if (!isCombat && !isElite) return;

        var pool = isCombat ? enemyPoolCombat : enemyPoolElite;
        if (pool == null || pool.Count == 0) return;

        var doorWorlds = new List<Vector3Int>();
        foreach (var d in myRoom.doors) doorWorlds.Add(d.worldTile);

        var candidates = CollectEnemyCandidateCells(myRoom, doorWorlds);
        if (candidates.Count == 0) return;

        int seed = myRoom.origin.x * 73856093 ^ myRoom.origin.y * 19349663 ^ myRoom.origin.z * 83492791 ^ 0xE11E;
        var rng = new System.Random(seed);

        int want = isCombat
            ? Mathf.Clamp(rng.Next(enemyCountRangeCombat.x, enemyCountRangeCombat.y + 1), 0, candidates.Count)
            : Mathf.Clamp(rng.Next(enemyCountRangeElite.x, enemyCountRangeElite.y + 1), 0, candidates.Count);

        ShuffleInPlaceDeterministic(candidates, rng);

        var taken = new HashSet<Vector3Int>();
        int placed = 0;
        for (int i = 0; i < candidates.Count && placed < want; i++)
        {
            var cell = candidates[i];
            if (HasReservedInRadius(taken, cell, minEnemySpacing)) continue;

            var pf = pool[rng.Next(0, pool.Count)];
            if (!pf) continue;

            var pos = dungeonGrid.GetCellCenterWorld(cell);
            var go = Instantiate(pf, myRoom.go.transform);
            go.transform.position = pos;

            ReserveInRadius(taken, cell, minEnemySpacing);
            placed++;
        }
    }

    //플레이어가 방에 진입했을 때
    public void OnPlayerEntered(Player player, Bounds worldBounds)
    {
        roomBounds = worldBounds;
        cleared = false;

        //문 잠금
        var placer = FindObjectOfType<RoomPlacer>();
        if (placer && myRoom != null) placer.LockRoom(myRoom);

        //전투 On (락온/스캔용 Bounds 전달)
        if (player) player.SetCombat(true, roomBounds);

        //적 목록 구성 & 등록
        BeginCombat();

        //적이 없으면 즉시 클리어
        if (aliveEnemyCount <= 0) UnlockAndEnd(player);
    }

    //방 안의 적 수집 → 목록에 등록 + 활성화
    private void BeginCombat()
    {
        UnsubscribeEnemies();

        //방 Bounds 안에서 Enemy 레이어만 수집
        var c = (Vector2)roomBounds.center;
        var s = (Vector2)roomBounds.size;
        var hits = Physics2D.OverlapBoxAll(c, s, 0f, ConstClass.Masks.Enemy);

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i].GetComponent<Health>();
            if (!h)
            {
                var p = hits[i].transform.parent;
                if (p) h = p.GetComponent<Health>();
            }
            if (!h) continue;

            enemies.Add(h);
            h.OnDeath += OnEnemyDeath;
        }
        aliveEnemyCount = enemies.Count;

        //이 방의 적 활성화
        SetEnemyActiveAll(true);
    }

    //적이 사망했을 때
    private void OnEnemyDeath()
    {
        if (cleared) return;
        aliveEnemyCount--;
        if (aliveEnemyCount <= 0)
        {
            var player = FindObjectOfType<Player>();
            UnlockAndEnd(player);
        }
    }

    //문 열기 + 전투 종료 + 비활성화 + 적 목록 정리
    private void UnlockAndEnd(Player player)
    {
        cleared = true;

        var placer = FindObjectOfType<RoomPlacer>();
        if (placer && myRoom != null) placer.UnlockRoom(myRoom);

        if (player) player.SetCombat(false);

        //이 방의 적 비활성화
        SetEnemyActiveAll(false);

        UnsubscribeEnemies();
    }

    //방의 적 활성화/비활성화
    private void SetEnemyActiveAll(bool active)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var h = enemies[i];
            if (!h) continue;
            var e = h.GetComponentInParent<Enemy>();
            if (e) e.SetEncounterActive(active);
        }
    }

    //적 목록/이벤트 정리
    private void UnsubscribeEnemies()
    {
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i] != null) enemies[i].OnDeath -= OnEnemyDeath;

        enemies.Clear();
        aliveEnemyCount = 0;
    }

    #region 스폰 후보 수집
    private List<Vector3Int> CollectEnemyCandidateCells(PlacedRoom pr, List<Vector3Int> doorWorlds)
    {
        var res = new List<Vector3Int>();
        if (pr == null || pr.go == null) return res;

        var forbid = new HashSet<Vector3Int>();
        foreach (var wc in doorWorlds)
            for (int dx = -minEnemyDistanceFromDoor; dx <= minEnemyDistanceFromDoor; dx++)
                for (int dy = -minEnemyDistanceFromDoor; dy <= minEnemyDistanceFromDoor; dy++)
                    forbid.Add(new Vector3Int(wc.x + dx, wc.y + dy, 0));

        var wallsMap = FindWallsMap(pr);

        foreach (var worldCell in pr.cells)
        {
            if (forbid.Contains(worldCell)) continue;
            if (IsNearWallLocal(wallsMap, worldCell, minEnemyDistanceFromWall)) continue;
            res.Add(worldCell);
        }
        return res;
    }

    private static Tilemap FindWallsMap(PlacedRoom pr)
    {
        if (pr == null || pr.go == null) return null;
        var maps = pr.go.GetComponentsInChildren<Tilemap>(true);
        foreach (var m in maps) if (m && m.name == "Tilemap_Walls") return m;
        return null;
    }

    private bool IsNearWallLocal(Tilemap wallsMap, Vector3Int worldCell, int r)
    {
        if (!wallsMap || dungeonGrid == null || r <= 0) return false;
        var worldPos = dungeonGrid.CellToWorld(worldCell);
        var local = wallsMap.WorldToCell(worldPos);

        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                if (wallsMap.HasTile(new Vector3Int(local.x + dx, local.y + dy, 0)))
                    return true;

        return false;
    }
    #endregion

    #region 보조 유틸
    private static void ShuffleInPlaceDeterministic<T>(List<T> a, System.Random rng)
    {
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }
    private static bool HasReservedInRadius(HashSet<Vector3Int> taken, Vector3Int c, int r)
    {
        if (r <= 0 || taken.Count == 0) return taken.Contains(c);
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                if (taken.Contains(new Vector3Int(c.x + dx, c.y + dy, 0)))
                    return true;
        return false;
    }
    private static void ReserveInRadius(HashSet<Vector3Int> taken, Vector3Int c, int r)
    {
        if (r <= 0) { taken.Add(c); return; }
        for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                taken.Add(new Vector3Int(c.x + dx, c.y + dy, 0));
    }
    #endregion
}