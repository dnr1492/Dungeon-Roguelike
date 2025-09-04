using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlacedRoom
{
    public GameObject go;
    public Vector3Int origin;  //전역 그리드 원점 (배치 위치)
    public List<PlacedDoor> doors = new();
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

    private readonly int attachCount = 4;  //시작 방 외에 추가할 방 개수
    private readonly int hallMin = 5;      //복도 최소 길이(타일)
    private readonly int hallMax = 10;     //복도 최대 길이(타일)

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        //초기화
        ClearChildren(roomsRoot);
        placed.Clear();

        //1) (전역 그리드 원점에) 시작 방 배치
        var start = PlaceRoomAt(startRoomPrefab, Vector3Int.zero);
        placed.Add(start);

        //2) 지정 개수만큼 이어붙이기
        for (int i = 0; i < attachCount; i++)
        {
            var nextPrefab = candidateRooms[Random.Range(0, candidateRooms.Count)];
            if (!TryAttachRoom(nextPrefab))
            {
                Debug.Log("[RoomPlacer] 붙일 호환 도어가 없음. 다음 시도.");
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

            //폭 동일 + 반대 방향만 후보
            var candidates = newAnchors.Where(b =>
                b.width == a.anchor.width &&
                DoorAnchor.Opposite(a.anchor.direction) == b.direction);

            foreach (var b in candidates)
            {
                var bWorldCenter = b.GetWorldCenter();
                var bLocalCell = dungeonGrid.WorldToCell(bWorldCenter);
                var origin = a.worldTile - (Vector3Int)new Vector2Int(bLocalCell.x, bLocalCell.y);

                //복도 길이만큼, 출발 문 방향으로 방을 더 밀어낸다
                int L = Random.Range(hallMin, hallMax + 1);
                origin += DirToVec(a.anchor.direction) * L;

                Destroy(preview);
                var placed = PlaceRoomAt(newRoomPrefab, origin);

                var placedB = FindMatchingDoor(placed, b);
                if (placedB == null)
                {
                    Debug.Log("[RoomPlacer] 새 방에서 매칭되는 문을 찾지 못함");
                    Destroy(placed.go);
                    return false;
                }

                //보정을 '맞닿기'가 아니라 'L칸 떨어진 위치'에 맞춤
                var desiredTo = a.worldTile + DirToVec(a.anchor.direction) * L;
                var delta = desiredTo - placedB.worldTile;
                if (delta != Vector3Int.zero)
                {
                    placed.origin += delta;
                    placed.go.transform.position = dungeonGrid.CellToWorld(placed.origin);
                    RebuildPlacedDoors(placed);
                    placedB = FindMatchingDoor(placed, b);
                }

                if (painter != null)
                {
                    var from = (Vector2Int)a.worldTile;
                    var to = (Vector2Int)desiredTo;
                    painter.PaintCorridor(from, to, a.anchor.width, a.anchor.direction);
                }

                a.used = true;
                this.placed.Add(placed);
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

    //폭/방향 일치 + 가장 가까운 문을 선택 (동일 프리팹 내에서 대응되는 DoorAnchor를 찾는 보조)
    private PlacedDoor FindMatchingDoor(PlacedRoom placed, DoorAnchor srcAnchor)
    {
        var cands = placed.doors.Where(d => d.anchor.width == srcAnchor.width &&
                                            d.anchor.direction == srcAnchor.direction).ToList();
        if (cands.Count == 0) return null;

        //가장 가까운 것 선택 (대부분 한 개만 존재)
        cands.Sort((x, y) => (x.worldTile - placed.origin).sqrMagnitude
                           .CompareTo((y.worldTile - placed.origin).sqrMagnitude));
        return cands[0];
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
}