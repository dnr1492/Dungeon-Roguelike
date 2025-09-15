using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomEncounter : MonoBehaviour
{
    [Header("Enemy Pools")]
    [SerializeField] List<GameObject> enemyPoolCombat;
    [SerializeField] List<GameObject> enemyPoolElite;

    #region �� ���� ������
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

    //���� ����
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

    //�÷��̾ �濡 �������� ��
    public void OnPlayerEntered(Player player, Bounds worldBounds)
    {
        roomBounds = worldBounds;
        cleared = false;

        //�� ���
        var placer = FindObjectOfType<RoomPlacer>();
        if (placer && myRoom != null) placer.LockRoom(myRoom);

        //���� On (����/��ĵ�� Bounds ����)
        if (player) player.SetCombat(true, roomBounds);

        //�� ��� ���� & ���
        BeginCombat();

        //���� ������ ��� Ŭ����
        if (aliveEnemyCount <= 0) UnlockAndEnd(player);
    }

    //�� ���� �� ���� �� ��Ͽ� ��� + Ȱ��ȭ
    private void BeginCombat()
    {
        UnsubscribeEnemies();

        //�� Bounds �ȿ��� Enemy ���̾ ����
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

        //�� ���� �� Ȱ��ȭ
        SetEnemyActiveAll(true);
    }

    //���� ������� ��
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

    //�� ���� + ���� ���� + ��Ȱ��ȭ + �� ��� ����
    private void UnlockAndEnd(Player player)
    {
        cleared = true;

        var placer = FindObjectOfType<RoomPlacer>();
        if (placer && myRoom != null) placer.UnlockRoom(myRoom);

        if (player) player.SetCombat(false);

        //�� ���� �� ��Ȱ��ȭ
        SetEnemyActiveAll(false);

        UnsubscribeEnemies();
    }

    //���� �� Ȱ��ȭ/��Ȱ��ȭ
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

    //�� ���/�̺�Ʈ ����
    private void UnsubscribeEnemies()
    {
        for (int i = 0; i < enemies.Count; i++)
            if (enemies[i] != null) enemies[i].OnDeath -= OnEnemyDeath;

        enemies.Clear();
        aliveEnemyCount = 0;
    }

    #region ���� �ĺ� ����
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

    #region ���� ��ƿ
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