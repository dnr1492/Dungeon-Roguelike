using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlacedRoom
{
    public GameObject go;
    public Vector3Int origin;  //전역 그리드 원점 (배치 위치)
    public List<PlacedDoor> doors = new();
    public List<Vector3Int> cells = new List<Vector3Int>();  //이 방이 점유하는 전역 그리드 셀 목록 (겹침 검사 / 등록용)
    public int depth;
}

public class PlacedDoor
{
    public PlacedRoom room;
    public RoomDoorAnchor anchor;  //방향/폭 정보용
    public Vector3Int worldTile;  //문 중앙 타일 (전역 그리드 기준)
    public bool used;
}

//방 개수 범위
[System.Serializable]
public struct QuotaRange
{
    public int min;
    public int max;
}

public struct QuotaMeta
{
    public QuotaRange combatRooms;  //예: 3~5
    public QuotaRange eliteRooms;   //예: 1~3
    public int bossRooms;           //1개 고정
    public QuotaRange shopRooms;    //예: 1~2
    public QuotaRange eventRooms;   //예: 2~3
}

public struct BacktrackStep
{
    public PlacedRoom parent;      //부모 방
    public PlacedDoor parentDoor;  //부모의 사용된 문 (롤백 시 되돌림)
    public PlacedRoom child;       //새로 붙인 방
    public string tag;             //새 방 타입

    public Vector2Int hallFrom;  //복도 from/to (최종 페인트용)
    public Vector2Int hallTo;
    public int hallWidth;

    public List<Vector3Int> addedRoomCells;  //occupied에 추가한 셀(방)
    public List<Vector3Int> addedHallCells;  //occupied에 추가한 셀(복도)

    //상태 복구용
    public bool parentDoorPrevUsed;
    public int prevConsecutiveCombat;
    public int prevPlaceStep;
    public int prevLastSpecialStep;
    public string prevLastPlacedTag;
}

/// <summary>
/// 프리팹 RoomDoorAnchor를 이용해 방을 배치
/// 복도는 최종 확정 시 일괄 페인트
/// 단, 복도 페인팅은 여기서 하지 않음 (연결만 수행)
/// </summary>
public class RoomPlacer : MonoBehaviour
{
    [SerializeField] Grid dungeonGrid;                 //DungeonRoot/Grid (전역 그리드)
    [SerializeField] Transform roomsRoot;              //DungeonRoot/RoomsRoot
    [SerializeField] GameObject startRoomPrefab;       //시작 방 프리팹
    [SerializeField] List<GameObject> candidateRooms;  //추가할 방 프리팹들
    [SerializeField] RoomCorridorPainter painter;      //복도 페인팅용
    [SerializeField] RoomPropThemeDB themeDB;

    public Transform CurRooomsRoot => roomsRoot;

    private readonly List<PlacedRoom> placed = new();
    private readonly HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();  //전역 그리드 점유 셀 (방 + 복도)
    private readonly int hallMin = 10;  //복도 최소 길이(타일)
    private readonly int hallMax = 10;  //복도 최대 길이(타일)
    private readonly Dictionary<PlacedRoom, HashSet<PlacedRoom>> graph = new();
    private readonly HashSet<PlacedDoor> deadDoors = new HashSet<PlacedDoor>();  //더이상 시도하지 않을 도어
    private readonly HashSet<PlacedRoom> deadRooms = new HashSet<PlacedRoom>();  //롤백으로 삭제된 방 (그 방의 도어는 무시)
    private readonly Dictionary<GameObject, List<Vector3Int>> cellsCache = new();  //캐시: 프리팹 → 타일셀
    private readonly Dictionary<GameObject, List<(DoorDir dir, int width, Vector3Int localCell)>> anchorsCache = new();  //캐시: 프리팹 → 앵커 (방향, 폭, 로컬셀)
    private readonly float specialBackoffProb = 0.6f;  //Shop ↔ Event 연속 체인 억제 확률
    private readonly int combatRunLimit = 3;  //전투가 이 횟수 이상 연속되면 특수방(상점/이벤트) 강제 삽입
    private readonly int shopMinDepthFromStart = 2;  //시작방으로부터 상점 최소 깊이(스텝)
    private readonly int eventMinDepthFromStart = 2;  //시작방으로부터 이벤트 최소 깊이(스텝)

    private int placeStep = 0;  //배치 진행도 - 전체 배치 누적 카운터
    private int consecutiveCombat = 0;  //배치 히스토리 - 전투 연속 카운터
    private string lastPlacedTag = null;
    private int lastSpecialStep = -9999;

    #region 쿼터를 만족할 때까지 방을 증분 배치하고, 막히면 백트래킹으로 경로 재탐색
    //던전 배치
    public void GenerateWithQuota(QuotaMeta meta)
    {
        ClearChildren(roomsRoot);
        placed.Clear();
        occupied.Clear();
        deadDoors.Clear();
        deadRooms.Clear();
        graph.Clear();
        placeStep = 0;
        consecutiveCombat = 0;
        lastPlacedTag = null;
        lastSpecialStep = -9999;

        var start = PlaceRoomAt(startRoomPrefab, Vector3Int.zero, 0);
        placed.Add(start);
        EnsureGraphNode(start);

        var remain = new Dictionary<string, int>
        {
            { Roomkind.CombatRoom.ToString(), SampleInRange(meta.combatRooms) },
            { Roomkind.EliteRoom.ToString(),  SampleInRange(meta.eliteRooms)  },
            { Roomkind.EventRoom.ToString(),  SampleInRange(meta.eventRooms)  },
            { Roomkind.ShopRoom.ToString(),   SampleInRange(meta.shopRooms)   },
            { Roomkind.BossRoom.ToString(),   Mathf.Max(0, meta.bossRooms)    },
        };

        var pool = new Dictionary<string, List<GameObject>>
        {
            { Roomkind.CombatRoom.ToString(), FilterCandidatesByTag(Roomkind.CombatRoom.ToString()) },
            { Roomkind.EliteRoom.ToString(),  FilterCandidatesByTag(Roomkind.EliteRoom.ToString())  },
            { Roomkind.EventRoom.ToString(),  FilterCandidatesByTag(Roomkind.EventRoom.ToString())  },
            { Roomkind.ShopRoom.ToString(),   FilterCandidatesByTag(Roomkind.ShopRoom.ToString())   },
            { Roomkind.BossRoom.ToString(),   FilterCandidatesByTag(Roomkind.BossRoom.ToString())   },
        };

        var q = new Queue<PlacedDoor>();
        foreach (var d in start.doors) if (!d.used) q.Enqueue(d);

        var steps = new List<BacktrackStep>();
        var doorTry = new Dictionary<PlacedDoor, int>();

        int safety = 40000;
        int lastProgress = 0;
        int stagnation = 0;

        while (safety-- > 0 && HasAnyRemain(remain))
        {
            if (q.Count == 0)
            {
                RefillFrontierQueue(q);

                if (q.Count == 0)
                {
                    int k = 3;
                    bool opened = false;
                    while (steps.Count > 0 && q.Count == 0)
                    {
                        k = Mathf.Min(k, steps.Count);
                        if (!RollbackSomeSteps(steps, remain, k, q)) break;
                        if (q.Count > 0) { opened = true; break; }
                        k *= 2;
                    }
                    if (!opened && q.Count == 0) break;
                }
            }

            var a = q.Dequeue();
            if (a == null || a.used || a.room == null) continue;
            if (deadDoors.Contains(a)) continue;
            if (deadRooms.Contains(a.room)) continue;

            var candTypes = BuildCandidateTypesForDoor(a, remain, pool);
            if (candTypes.Count == 0)
            {
                if (!doorTry.ContainsKey(a)) doorTry[a] = 0;
                if (++doorTry[a] <= 10) q.Enqueue(a);
                else deadDoors.Add(a);
                continue;
            }

            ApplyDiversityGatesForDoor(a, candTypes, remain);

            bool placedOk = false;
            int tagTrials = Mathf.Min(3, candTypes.Count);

            for (int t = 0; t < tagTrials && !placedOk; t++)
            {
                var tag = WeightedPickByRemain(candTypes, remain);
                candTypes.Remove(tag);

                var prefabs = pool[tag];
                if (prefabs == null || prefabs.Count == 0) continue;

                int perTypeAttempts = Mathf.Min(8, prefabs.Count);
                for (int k = 0; k < perTypeAttempts && !placedOk; k++)
                {
                    var prefab = prefabs[Random.Range(0, prefabs.Count)];
                    int candDepth = a.room.depth + 1;

                    if ((tag == Roomkind.ShopRoom.ToString() && candDepth < shopMinDepthFromStart) ||
                        (tag == Roomkind.EventRoom.ToString() && candDepth < eventMinDepthFromStart))
                        continue;

                    if ((tag == Roomkind.EventRoom.ToString() || tag == Roomkind.ShopRoom.ToString()) &&
                        ViolatesSameTypeWithin2(a.room, tag))
                        continue;

                    if (TryAttachRoom_AtDoorWithPrefab(a, prefab, tag, out var step))
                    {
                        steps.Add(step);
                        remain[tag] = Mathf.Max(0, remain[tag] - 1);

                        var newRoom = step.child;
                        EnsureGraphNode(newRoom);
                        AddGraphEdge(a.room, newRoom);

                        var newDoors = newRoom.doors.Where(d => !d.used).ToList();
                        if (tag == Roomkind.EventRoom.ToString() && newDoors.Count > 0)
                        {
                            var rest = q.ToArray(); q.Clear();
                            foreach (var nd in newDoors) q.Enqueue(nd);
                            foreach (var r in rest) q.Enqueue(r);
                        }
                        else
                        {
                            foreach (var d in newDoors) if (!d.used) q.Enqueue(d);
                        }

                        placedOk = true;
                    }
                }
            }

            if (!placedOk)
            {
                if (!doorTry.ContainsKey(a)) doorTry[a] = 0;
                if (++doorTry[a] <= 10) q.Enqueue(a);
                else deadDoors.Add(a);
            }

            if (steps.Count > lastProgress) { lastProgress = steps.Count; stagnation = 0; }
            else if (++stagnation >= 200)
            {
                stagnation = 0;
                int k = Mathf.Clamp(steps.Count / 2, 3, steps.Count);
                if (k > 0) RollbackSomeSteps(steps, remain, k, q);
            }
        }

        PaintCorridorsFromSteps(steps);
        FinalizeUnusedDoors();

        foreach (var kv in remain)
        {
            if (kv.Value > 0) Debug.Log($"[RoomPlacer] quota 미달: {kv.Key} {kv.Value}개 부족");
            else Debug.Log($"[RoomPlacer] quota 완료: {kv.Key}");
        }
    }

    //도어 기준 후보 타입 목록 생성 (잔여량 > 0, Pool 보유, 폭/방향 호환)
    private List<string> BuildCandidateTypesForDoor(PlacedDoor a, Dictionary<string, int> remain, Dictionary<string, List<GameObject>> pool)
    {
        var list = new List<string>();
        foreach (var kv in remain)
        {
            var tag = kv.Key;
            if (kv.Value <= 0) continue;
            if (!pool.ContainsKey(tag) || pool[tag] == null || pool[tag].Count == 0) continue;

            bool anyWidth = false;
            foreach (var pf in pool[tag])
            {
                var anchors = GetAnchorsCached(pf);
                if (anchors.Any(z => z.width == a.anchor.width && RoomDoorAnchor.Opposite(a.anchor.direction) == z.dir))
                { anyWidth = true; break; }
            }
            if (!anyWidth) continue;

            list.Add(tag);
        }
        return list;
    }

    //전투 연속 제한 및 특수 연쇄 억제 정책을 후보 타입 리스트에 적용
    private void ApplyDiversityGatesForDoor(PlacedDoor a, List<string> candTypes, Dictionary<string, int> remain)
    {
        if (candTypes == null || candTypes.Count == 0) return;

        string COMBAT = Roomkind.CombatRoom.ToString();
        if (consecutiveCombat >= combatRunLimit && candTypes.Contains(COMBAT))
        {
            bool hasAlt = candTypes.Any(t => t != COMBAT && remain.TryGetValue(t, out var r) && r > 0);
            if (hasAlt) candTypes.Remove(COMBAT);
        }

        bool justPlacedSpecial = (placeStep - lastSpecialStep) <= 1;
        if (justPlacedSpecial && Random.value < Mathf.Clamp01(specialBackoffProb))
        {
            string SHOP = Roomkind.ShopRoom.ToString();
            string EVENT = Roomkind.EventRoom.ToString();

            if (lastPlacedTag == SHOP && candTypes.Contains(EVENT))
            {
                bool hasAlt = candTypes.Any(t => t != EVENT && remain.TryGetValue(t, out var r) && r > 0);
                if (hasAlt) candTypes.Remove(EVENT);
            }
            else if (lastPlacedTag == EVENT && candTypes.Contains(SHOP))
            {
                bool hasAlt = candTypes.Any(t => t != SHOP && remain.TryGetValue(t, out var r) && r > 0);
                if (hasAlt) candTypes.Remove(SHOP);
            }
        }
    }

    //잔여량 가중 + 지터 기반 타입 가중치 랜덤 선택
    private string WeightedPickByRemain(List<string> candTypes, Dictionary<string, int> remain)
    {
        float sum = 0f;
        var w = new float[candTypes.Count];
        for (int i = 0; i < candTypes.Count; i++)
        {
            remain.TryGetValue(candTypes[i], out int r);
            float jitter = Random.value;
            w[i] = Mathf.Max(0.0001f, r + 0.25f * jitter);
            sum += w[i];
        }
        float pick = Random.value * sum, acc = 0f;
        for (int i = 0; i < candTypes.Count; i++)
        {
            acc += w[i];
            if (pick <= acc) return candTypes[i];
        }
        return candTypes[candTypes.Count - 1];
    }
    #endregion

    #region 문-문 정합 배치와 실패 시 롤백을 묶음으로 관리
    //기준 문 a에 프리팹을 정합 배치
    private bool TryAttachRoom_AtDoorWithPrefab(PlacedDoor a, GameObject prefab, string tag, out BacktrackStep step)
    {
        step = default;
        if (a.room == null || prefab == null) return false;

        var anchors = GetAnchorsCached(prefab);
        if (anchors.Count == 0) return false;

        var candAnch = anchors.Where(b => b.width == a.anchor.width &&
                                          RoomDoorAnchor.Opposite(a.anchor.direction) == b.dir).ToList();
        if (candAnch.Count == 0) return false;

        var Ls = new List<int>(); for (int L = hallMin; L <= hallMax; L++) Ls.Add(L);
        ShuffleInPlace(Ls);

        foreach (var b in candAnch)
        {
            foreach (var L in Ls)
            {
                var stepVec = DirToVec(a.anchor.direction);
                var desiredTo = a.worldTile + stepVec * L;

                if (!TryGetAlignedDir((Vector2Int)a.worldTile, (Vector2Int)desiredTo, out var dirAligned))
                    continue;

                var finalOrigin = desiredTo - b.localCell;

                var roomCells = GetRoomCellsCached(prefab).Select(c => c + finalOrigin).ToList();
                var hallCells = EnumerateCorridorCellsNoFrom((Vector2Int)a.worldTile, (Vector2Int)desiredTo, a.anchor.width, dirAligned);
                if (hallCells.Count == 0) continue;
                hallCells.RemoveAll(c => a.room.cells.Contains(c));

                if (HasOverlap(roomCells) || HasOverlap(hallCells)) continue;

                int candDepth = a.room.depth + 1;

                if ((prefab.CompareTag(Roomkind.ShopRoom.ToString()) && candDepth < shopMinDepthFromStart) ||
                    (prefab.CompareTag(Roomkind.EventRoom.ToString()) && candDepth < eventMinDepthFromStart))
                    continue;

                var placedRoom = PlaceRoomAt(prefab, finalOrigin, candDepth);

                bool prevUsed = a.used;
                a.used = true;

                var bPlaced = placedRoom.doors.FirstOrDefault(d =>
                    d.worldTile == desiredTo && d.anchor.direction == b.dir && d.anchor.width == b.width);
                if (bPlaced != null) bPlaced.used = true;

                foreach (var c in roomCells) occupied.Add(c);
                foreach (var c in hallCells) occupied.Add(c);

                placed.Add(placedRoom);

                step = new BacktrackStep
                {
                    parent = a.room,
                    parentDoor = a,
                    child = placedRoom,
                    tag = tag,
                    hallFrom = (Vector2Int)a.worldTile,
                    hallTo = (Vector2Int)desiredTo,
                    hallWidth = a.anchor.width,
                    addedRoomCells = roomCells,
                    addedHallCells = hallCells,
                    parentDoorPrevUsed = prevUsed,
                    prevConsecutiveCombat = consecutiveCombat,
                    prevPlaceStep = placeStep,
                    prevLastSpecialStep = lastSpecialStep,
                    prevLastPlacedTag = lastPlacedTag
                };

                placeStep++;
                lastPlacedTag = prefab.tag;

                if (lastPlacedTag == Roomkind.CombatRoom.ToString()) consecutiveCombat++;
                else consecutiveCombat = 0;

                if (lastPlacedTag == Roomkind.ShopRoom.ToString() || lastPlacedTag == Roomkind.EventRoom.ToString())
                    lastSpecialStep = placeStep;

                return true;
            }
        }
        return false;
    }

    //최근 k개 부착 스텝을 되돌려 탐색 경로 재개
    private bool RollbackSomeSteps(List<BacktrackStep> steps, Dictionary<string, int> remain, int k, Queue<PlacedDoor> q)
    {
        if (steps == null || steps.Count == 0) return false;

        int cnt = Mathf.Min(k, steps.Count);
        for (int i = 0; i < cnt; i++)
        {
            var st = steps[steps.Count - 1];
            steps.RemoveAt(steps.Count - 1);

            if (remain.ContainsKey(st.tag)) remain[st.tag] += 1;

            RemoveGraphEdge(st.parent, st.child);
            deadRooms.Add(st.child);

            if (st.child != null)
            {
                if (st.child.go != null) Destroy(st.child.go);
                placed.Remove(st.child);
            }

            foreach (var c in st.addedRoomCells) occupied.Remove(c);
            foreach (var c in st.addedHallCells) occupied.Remove(c);

            st.parentDoor.used = st.parentDoorPrevUsed;

            consecutiveCombat = st.prevConsecutiveCombat;
            placeStep = st.prevPlaceStep;
            lastSpecialStep = st.prevLastSpecialStep;
            lastPlacedTag = st.prevLastPlacedTag;

            if (st.parent != null && !deadRooms.Contains(st.parent) && !st.parentDoor.used)
                q.Enqueue(st.parentDoor);
        }
        return true;
    }

    //현재 살아있는 방들의 미사용 도어를 프런티어 큐에 채움
    private void RefillFrontierQueue(Queue<PlacedDoor> q)
    {
        foreach (var r in placed)
            if (r != null && !deadRooms.Contains(r))
                foreach (var d in r.doors)
                    if (!d.used && !deadDoors.Contains(d))
                        q.Enqueue(d);
    }
    #endregion

    #region 배치된 방들의 인접 관계를 유지/조회
    //그래프 노드 보장
    private void EnsureGraphNode(PlacedRoom r)
    {
        if (r == null) return;
        if (!graph.ContainsKey(r)) graph[r] = new HashSet<PlacedRoom>();
    }

    //양방향 간선 추가
    private void AddGraphEdge(PlacedRoom a, PlacedRoom b)
    {
        if (a == null || b == null) return;
        EnsureGraphNode(a); EnsureGraphNode(b);
        graph[a].Add(b); graph[b].Add(a);
    }

    //양방향 간선 제거
    private void RemoveGraphEdge(PlacedRoom a, PlacedRoom b)
    {
        if (a == null || b == null) return;
        if (graph.TryGetValue(a, out var sa)) sa.Remove(b);
        if (graph.TryGetValue(b, out var sb)) sb.Remove(a);
    }

    //인접 방 열거
    private IEnumerable<PlacedRoom> GetNeighbors(PlacedRoom r)
    {
        if (r == null) yield break;
        if (!graph.TryGetValue(r, out var set)) yield break;
        foreach (var x in set) yield return x;
    }

    //parent로부터 그래프 거리 ≤ 2 범위에 동일 tag 존재 여부
    private bool ViolatesSameTypeWithin2(PlacedRoom parent, string tag)
    {
        if (parent == null) return false;

        var parentGO = parent.go;
        if (parentGO != null && parentGO.tag == tag) return true;

        foreach (var nb in GetNeighbors(parent))
        {
            if (nb == null) continue;
            var nbGO = nb.go;
            if (nbGO != null && nbGO.tag == tag) return true;
        }
        return false;
    }
    #endregion

    #region 프리팹 분석 결과(타일 셀, 문 앵커) 캐싱
    //프리팹의 타일 셀 전역 좌표 목록 캐시
    private List<Vector3Int> GetRoomCellsCached(GameObject prefab)
    {
        if (!prefab) return new List<Vector3Int>();
        if (cellsCache.TryGetValue(prefab, out var cached)) return cached;

        var cells = new List<Vector3Int>();
        var tms = prefab.GetComponentsInChildren<Tilemap>(true);
        foreach (var tm in tms)
        {
            if (!tm) continue;
            var b = tm.cellBounds;
            foreach (var pos in b.allPositionsWithin) if (tm.HasTile(pos)) cells.Add(pos);
        }
        cellsCache[prefab] = cells;
        return cells;
    }

    //프리팹의 문 앵커 정보 캐시 (방향, 폭, 로컬셀)
    private List<(DoorDir dir, int width, Vector3Int localCell)> GetAnchorsCached(GameObject prefab)
    {
        if (!prefab) return new List<(DoorDir, int, Vector3Int)>();
        if (anchorsCache.TryGetValue(prefab, out var cached)) return cached;

        var res = new List<(DoorDir, int, Vector3Int)>();
        var grid = prefab.GetComponentInChildren<Grid>(true);
        var anchors = prefab.GetComponentsInChildren<RoomDoorAnchor>(true);
        foreach (var a in anchors)
        {
            var local = grid ? grid.WorldToCell(a.GetWorldCenter()) : Vector3Int.zero;
            res.Add((a.direction, a.width, local));
        }
        anchorsCache[prefab] = res;
        return res;
    }
    #endregion

    #region 확정 스텝을 기반으로 복도 페인트, 미사용 도어 마감
    //백트래킹 스텝 순서대로 복도 일괄 페인트
    private void PaintCorridorsFromSteps(List<BacktrackStep> steps)
    {
        if (painter == null || steps == null) return;
        for (int i = 0; i < steps.Count; i++)
        {
            var s = steps[i];
            painter.PaintCorridor(s.hallFrom, s.hallTo, s.hallWidth);
        }
    }

    //미사용 도어를 벽으로 막고 앵커 제거
    private void FinalizeUnusedDoors()
    {
        if (painter == null) return;

        foreach (var pr in placed)
        {
            var roomWalls = FindWallsMap(pr);
            var stillUnused = new List<PlacedDoor>();

            foreach (var d in pr.doors)
            {
                if (d.used) continue;

                int half = d.anchor.width / 2;
                if (d.anchor.direction == DoorDir.North || d.anchor.direction == DoorDir.South)
                {
                    for (int o = -half - 1; o <= half + 1; o++)
                    {
                        var worldCell = new Vector2Int(d.worldTile.x + o, d.worldTile.y);
                        painter.SetWall(roomWalls, worldCell, d.anchor.direction, 1);
                    }
                }
                else
                {
                    for (int o = -half - 1; o <= half + 1; o++)
                    {
                        var worldCell = new Vector2Int(d.worldTile.x, d.worldTile.y + o);
                        painter.SetWall(roomWalls, worldCell, d.anchor.direction, 1);
                    }
                }

                stillUnused.Add(d);
            }

            foreach (var d in stillUnused)
                if (d.anchor) Destroy(d.anchor.gameObject);
        }
    }

    //방 오브젝트에서 벽 타일맵 검색
    private Tilemap FindWallsMap(PlacedRoom pr)
    {
        if (pr == null || pr.go == null) return null;
        var maps = pr.go.GetComponentsInChildren<Tilemap>(true);
        foreach (var m in maps) if (m && m.name == "Tilemap_Walls") return m;
        return null;
    }

    //from 셀 제외 복도 셀 나열 (겹침 검사용)
    private List<Vector3Int> EnumerateCorridorCellsNoFrom(Vector2Int from, Vector2Int to, int width, DoorDir dir)
    {
        var res = new List<Vector3Int>();
        var step = DirToVec(dir);
        var len = Mathf.Abs((to - from).x) + Mathf.Abs((to - from).y);

        for (int i = 1; i <= len; i++)
        {
            var center = (Vector3Int)(from + i * new Vector2Int(step.x, step.y));
            Vector3Int ortho = (dir == DoorDir.North || dir == DoorDir.South) ? new Vector3Int(1, 0, 0) : new Vector3Int(0, 1, 0);
            foreach (int o in Offsets(width)) res.Add(center + ortho * o);
        }
        return res;
    }
    #endregion

    #region 방 인스턴스 생성, 도어/점유 셀 재구성, 도어 락/언락, 룸 찾기
    //프리팹을 전역 그리드 원점에 배치하고 도어/셀/장식 세팅
    private PlacedRoom PlaceRoomAt(GameObject prefab, Vector3Int origin, int depth)
    {
        var inst = Instantiate(prefab, roomsRoot);
        inst.name = prefab.name;
        inst.transform.position = dungeonGrid.CellToWorld(origin);

        var pr = new PlacedRoom { go = inst, origin = origin, depth = depth };
        RebuildPlacedDoors(pr);

        pr.cells = GetRoomCells(inst);
        foreach (var c in pr.cells) occupied.Add(c);

        TryPaintPropsForRoomConditional(pr);
        return pr;
    }

    //PlacedRoom 기준 도어 목록 재구성
    private void RebuildPlacedDoors(PlacedRoom pr)
    {
        pr.doors.Clear();
        var anchors = pr.go.GetComponentsInChildren<RoomDoorAnchor>(true);
        foreach (var a in anchors)
        {
            var worldCenter = a.GetWorldCenter();
            var worldCell = dungeonGrid.WorldToCell(worldCenter);
            pr.doors.Add(new PlacedDoor { room = pr, anchor = a, worldTile = worldCell });
        }
    }

    //현재 방의 실제 RoomDoorAnchor가 달린 문 위치만 게이트 닫기
    public void LockRoom(PlacedRoom pr)
    {
        if (painter == null || pr == null) return;

        RebuildPlacedDoors(pr);

        foreach (var d in pr.doors)
        {
            if (!d.anchor) continue;
            painter.SetDoorGate((Vector2Int)d.worldTile, d.anchor.direction, d.anchor.width, true);
        }
    }

    //현재 방의 실제 RoomDoorAnchor가 달린 문 위치만 게이트 열기
    public void UnlockRoom(PlacedRoom pr)
    {
        if (painter == null || pr == null) return;

        RebuildPlacedDoors(pr);

        foreach (var d in pr.doors)
        {
            if (!d.anchor) continue;
            painter.SetDoorGate((Vector2Int)d.worldTile, d.anchor.direction, d.anchor.width, false);
        }
    }

    public PlacedRoom FindPlacedRoomByInstance(GameObject instanceRoot)
    {
        if (instanceRoot == null) return null;
        foreach (var pr in placed) if (pr.go == instanceRoot) return pr;
        return null;
    }
    #endregion

    #region 전투/엘리트 방에 한해 테마 기반 장식 자동 페인트
    //조건 만족 시 결정적 시드 기반 장식 페인트
    private void TryPaintPropsForRoomConditional(PlacedRoom pr)
    {
        if (pr == null || pr.go == null) return;
        if (!IsAutoDecorTarget(pr.go)) return;

        var propPainter = pr.go.GetComponentInChildren<RoomPropPainter>(true);
        if (!propPainter) return;

        var doorWorlds = new List<Vector3Int>();
        foreach (var d in pr.doors) doorWorlds.Add(d.worldTile);

        int seed = pr.origin.x * 73856093 ^ pr.origin.y * 19349663 ^ pr.origin.z * 83492791;
        var rng = new System.Random(seed);

        var themeId = GameManager.Instance ? GameManager.Instance.CurrentThemeId : ThemeId.Dungeon;

        if (themeDB && themeDB.TryGet(themeId, out var ts))
            propPainter.ApplyThemeSet(ts, pr.go.tag);

        propPainter.PaintDeterministic(rng, doorWorlds, dungeonGrid);
    }

    //자동 장식 대상 방 판별 (전투/엘리트)
    private bool IsAutoDecorTarget(GameObject roomRoot)
    {
        if (!roomRoot) return false;
        if (roomRoot.CompareTag(Roomkind.CombatRoom.ToString())) return true;
        if (roomRoot.CompareTag(Roomkind.EliteRoom.ToString())) return true;
        return false;
    }
    #endregion

    #region 공통 유틸 집합 (필터, 샘플링, 충돌/방향 변환, 정리)
    private List<GameObject> FilterCandidatesByTag(string tagName)
    {
        var res = new List<GameObject>();
        foreach (var go in candidateRooms) if (go && go.CompareTag(tagName)) res.Add(go);
        return res;
    }

    private int SampleInRange(QuotaRange r)
    {
        int a = Mathf.Min(r.min, r.max);
        int b = Mathf.Max(r.min, r.max);
        return Random.Range(a, b + 1);
    }

    private bool HasOverlap(IEnumerable<Vector3Int> cells)
    {
        foreach (var c in cells) if (occupied.Contains(c)) return true;
        return false;
    }

    private List<Vector3Int> GetRoomCells(GameObject instanceRoot)
    {
        var list = new List<Vector3Int>();
        var tilemaps = instanceRoot.GetComponentsInChildren<Tilemap>(true);

        foreach (var tm in tilemaps)
        {
            var bounds = tm.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (!tm.HasTile(pos)) continue;
                var world = tm.CellToWorld(pos);
                var cell = dungeonGrid.WorldToCell(world);
                list.Add(cell);
            }
        }
        return list;
    }

    private IEnumerable<int> Offsets(int width)
    {
        int half = width / 2;
        if ((width & 1) == 1) { for (int o = -half; o <= half; o++) yield return o; }
        else { for (int o = -half; o < half; o++) yield return o; }
    }

    private Vector3Int DirToVec(DoorDir d)
    {
        return d == DoorDir.North ? new Vector3Int(0, 1, 0) :
               d == DoorDir.South ? new Vector3Int(0, -1, 0) :
               d == DoorDir.East ? new Vector3Int(1, 0, 0) :
                                    new Vector3Int(-1, 0, 0);
    }

    private bool TryGetAlignedDir(Vector2Int from, Vector2Int to, out DoorDir dir)
    {
        dir = DoorDir.North;
        var dx = to.x - from.x;
        var dy = to.y - from.y;
        if (dx == 0 && dy != 0) { dir = (dy > 0) ? DoorDir.North : DoorDir.South; return true; }
        if (dy == 0 && dx != 0) { dir = (dx > 0) ? DoorDir.East : DoorDir.West; return true; }
        return false;
    }

    private void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }

    private bool HasAnyRemain(Dictionary<string, int> remain)
    {
        foreach (var kv in remain) if (kv.Value > 0) return true;
        return false;
    }

    private void ShuffleInPlace<T>(List<T> a)
    {
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }
    #endregion
}