using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlacedRoom
{
    public GameObject go;
    public Vector3Int origin;  //전역 그리드 원점 (배치 위치)
    public List<PlacedDoor> doors = new();
    public List<Vector3Int> cells = new List<Vector3Int>();  //이 방이 점유하는 전역 그리드 셀 목록 (겹침 검사 / 등록용)
}

public class PlacedDoor
{
    public PlacedRoom room;
    public DoorAnchor anchor;  //방향/폭 정보용
    public Vector3Int worldTile;  //문 중앙 타일 (전역 그리드 기준)
    public bool used;
}

/// <summary>
/// 프리팹에 붙은 DoorAnchor를 읽어서
/// - 시작방을 배치하고
/// - 기존 방의 '미사용 도어'와 새 방의 '호환 도어(폭 동일 + 반대방향)'를 찾아
/// - 새 방을 정확히 맞닿도록 원점을 계산해 배치한다.
/// 복도 페인팅은 여기서 하지 않음 (연결만 수행)
/// </summary>
public class RoomPlacer : MonoBehaviour
{
    public Grid dungeonGrid;                 //DungeonRoot/Grid (전역 그리드)
    public Transform roomsRoot;              //DungeonRoot/RoomsRoot
    public GameObject startRoomPrefab;       //시작 방 프리팹
    public List<GameObject> candidateRooms;  //추가할 방 프리팹들
    public RoomCorridorPainter painter;      //복도 페인팅용

    private readonly List<PlacedRoom> placed = new();
    private readonly HashSet<Vector3Int> occupied = new HashSet<Vector3Int>();  //전역 그리드 점유 셀 (방 + 복도)

    private readonly int attachCount = 10;  //시작 방 외에 추가할 방 개수
    private readonly int hallMin = 5;       //복도 최소 길이(타일)
    private readonly int hallMax = 10;      //복도 최대 길이(타일)

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        //초기화
        ClearChildren(roomsRoot);
        placed.Clear();
        occupied.Clear();

        //1) (전역 그리드 원점에) 시작 방 배치
        var start = PlaceRoomAt(startRoomPrefab, Vector3Int.zero);
        placed.Add(start);

        //2) 지정 개수만큼 이어붙이기
        for (int i = 0; i < attachCount; i++)
        {
            var nextPrefab = candidateRooms[Random.Range(0, candidateRooms.Count)];
            if (!TryAttachRoom(nextPrefab))
            {
                Debug.Log("[RoomPlacer] 붙일 호환되는 문이 없음. 다음 시도.");
            }
        }
    }

    //기존의 '미사용 문'과 새 방의 '호환되는 문'을 찾아 배치
    private bool TryAttachRoom(GameObject newRoomPrefab)
    {
        foreach (var a in AllFreeDoors())
        {
            var preview = Instantiate(newRoomPrefab);
            var newAnchors = preview.GetComponentsInChildren<DoorAnchor>(true);

            var candidates = newAnchors.Where(b =>
                b.width == a.anchor.width &&
                DoorAnchor.Opposite(a.anchor.direction) == b.direction);

            foreach (var b in candidates)
            {
                //1) 후보 원점(originCandidate) 계산
                var bWorldCenter_Pre = b.GetWorldCenter();
                var bLocalCell_Pre = dungeonGrid.WorldToCell(bWorldCenter_Pre);
                var originCandidate = a.worldTile - (Vector3Int)new Vector2Int(bLocalCell_Pre.x, bLocalCell_Pre.y);

                //2) 복도 길이만큼 직선 오프셋
                int L = Random.Range(hallMin, hallMax + 1);
                originCandidate += DirToVec(a.anchor.direction) * L;

                //3) preview를 originCandidate 위치에 두고, b의 '문 중앙 셀' 재계산
                preview.transform.position = dungeonGrid.CellToWorld(originCandidate);
                var desiredTo = a.worldTile + DirToVec(a.anchor.direction) * L;
                var bWorldCell_AtCandidate = dungeonGrid.WorldToCell(b.GetWorldCenter());

                //4) 문 중심이 정확히 L칸 떨어지도록 최종 원점 보정 (finalOrigin)
                var delta = desiredTo - bWorldCell_AtCandidate;
                var finalOrigin = originCandidate + delta;

                //5) preview를 finalOrigin으로 옮겨서 실제 점유 셀을 수집
                preview.transform.position = dungeonGrid.CellToWorld(finalOrigin);

                //방 점유 셀 추출
                var roomCells = GetRoomCells(preview);

                //복도 점유 셀 계산 (직선)
                var hallCells = EnumerateCorridorCells((Vector2Int)a.worldTile,
                                                       (Vector2Int)desiredTo,
                                                       a.anchor.width,
                                                       a.anchor.direction);

                //겹침 검사 (방 vs 전역 / 복도 vs 전역)
                if (HasOverlap(roomCells) || HasOverlap(hallCells))
                {
                    //겹치면 패스
                    continue;
                }

                //실배치
                Destroy(preview);
                var placedRoom = PlaceRoomAt(newRoomPrefab, finalOrigin);

                //복도 페인팅 및 점유 등록
                if (painter != null)
                {
                    var from = (Vector2Int)a.worldTile;
                    var to = (Vector2Int)desiredTo;
                    painter.PaintCorridor(from, to, a.anchor.width, a.anchor.direction);
                }

                //복도 점유 등록
                foreach (var c in hallCells) occupied.Add(c);

                a.used = true;
                this.placed.Add(placedRoom);
                return true;
            }

            Destroy(preview);
        }

        return false;
    }

    private IEnumerable<PlacedDoor> AllFreeDoors()
    {
        foreach (var r in placed)
            foreach (var d in r.doors)
                if (!d.used) yield return d;
    }

    //새 방을 origin에 배치하고 DoorAnchor들을 스캔하여 worldTile을 계산
    private PlacedRoom PlaceRoomAt(GameObject prefab, Vector3Int origin)
    {
        var inst = Instantiate(prefab, roomsRoot);
        inst.name = prefab.name;
        inst.transform.position = dungeonGrid.CellToWorld(origin);

        var pr = new PlacedRoom { go = inst, origin = origin };
        RebuildPlacedDoors(pr);

        //점유 셀 계산 및 등록
        pr.cells = GetRoomCells(inst);
        foreach (var c in pr.cells) occupied.Add(c);

        return pr;
    }

    //배치된 방의 DoorAnchor들로 PlacedDoor 목록을 다시 구성
    private void RebuildPlacedDoors(PlacedRoom pr)
    {
        pr.doors.Clear();
        var anchors = pr.go.GetComponentsInChildren<DoorAnchor>(true);
        foreach (var a in anchors)
        {
            var worldCenter = a.GetWorldCenter();
            var worldCell = dungeonGrid.WorldToCell(worldCenter);
            pr.doors.Add(new PlacedDoor
            {
                room = pr,
                anchor = a,
                worldTile = worldCell
            });
        }
    }

    //DoorDir → 타일 좌표 단위 방향 벡터
    private Vector3Int DirToVec(DoorDir d)
    {
        return d == DoorDir.North ? new Vector3Int(0, 1, 0) :
               d == DoorDir.South ? new Vector3Int(0, -1, 0) :
               d == DoorDir.East ? new Vector3Int(1, 0, 0) :
                                     new Vector3Int(-1, 0, 0);
    }

    private void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    //특정 방의 문을 닫기
    public void LockRoom(PlacedRoom pr)
    {
        if (painter == null || pr == null) return;
        foreach (var d in pr.doors)
        {
            painter.SetDoorGate(
                (Vector2Int)d.worldTile,
                d.anchor.direction,
                d.anchor.width,
                true);
        }
    }
    
    //특정 방의 문을 열기
    public void UnlockRoom(PlacedRoom pr)
    {
        if (painter == null || pr == null) return;
        foreach (var d in pr.doors)
        {
            painter.SetDoorGate(
                (Vector2Int)d.worldTile,
                d.anchor.direction,
                d.anchor.width,
                false);
        }
    }

    //해당 GameObject가 속한 배치 방(PlacedRoom) 찾기
    public PlacedRoom FindPlacedRoomByInstance(GameObject instanceRoot)
    {
        if (instanceRoot == null) return null;
        foreach (var pr in placed)
        {
            if (pr.go == instanceRoot) return pr;
        }
        return null;
    }

    //미리보기/배치된 인스턴스의 '타일이 실제로 존재하는' 전역 셀을 수집
    private List<Vector3Int> GetRoomCells(GameObject instanceRoot)
    {
        var list = new List<Vector3Int>();
        var tilemaps = instanceRoot.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>(true);

        foreach (var tm in tilemaps)
        {
            //타일맵의 유효 영역을 압축하여 순회
            var bounds = tm.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (!tm.HasTile(pos)) continue;

                //이 타일의 월드 좌표 -> 던전 전역 그리드 셀
                var world = tm.CellToWorld(pos);
                var cell = dungeonGrid.WorldToCell(world);
                list.Add(cell);
            }
        }
        return list;
    }

    //선형 복도 구간의 점유 셀 나열 (폭 고려)
    private List<Vector3Int> EnumerateCorridorCells(Vector2Int from, Vector2Int to, int width, DoorDir dir)
    {
        var res = new List<Vector3Int>();

        var step = DirToVec(dir);
        var len = Mathf.Abs((to - from).x) + Mathf.Abs((to - from).y);  //직선이므로 L1 길이 == 실제 길이

        for (int i = 0; i <= len; i++)
        {
            var center = (Vector3Int)(from + i * (Vector2Int)new Vector2Int(step.x, step.y));

            //폭(w) 만큼 직교 방향으로 확장
            Vector3Int ortho = (dir == DoorDir.North || dir == DoorDir.South) ? new Vector3Int(1, 0, 0)
                                                                               : new Vector3Int(0, 1, 0);
            int half = width / 2;
            for (int o = -half; o <= half; o++)
            {
                res.Add(center + ortho * o);
            }
        }
        return res;
    }

    //전역 점유와 교차하는지
    private bool HasOverlap(IEnumerable<Vector3Int> cells)
    {
        foreach (var c in cells)
            if (occupied.Contains(c)) return true;
        return false;
    }
}