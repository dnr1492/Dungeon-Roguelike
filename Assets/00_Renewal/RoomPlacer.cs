using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlacedRoom
{
    public GameObject go;
    public Vector3Int origin;  //���� �׸��� ���� (��ġ ��ġ)
    public List<PlacedDoor> doors = new();
}

public class PlacedDoor
{
    public PlacedRoom room;
    public DoorAnchor anchor;  //����/�� ������
    public Vector3Int worldTile;  //�� �߾� Ÿ�� (���� �׸��� ����)
    public bool used;
}

/// <summary>
/// �����տ� ���� DoorAnchor�� �о
/// - ���۹��� ��ġ�ϰ�
/// - ���� ���� '�̻�� ����'�� �� ���� 'ȣȯ ����(�� ���� + �ݴ����)'�� ã��
/// - �� ���� ��Ȯ�� �´굵�� ������ ����� ��ġ�Ѵ�.
/// ���� �������� ���⼭ ���� ���� (���Ḹ ����)
/// </summary>
public class RoomPlacer : MonoBehaviour
{
    public Grid dungeonGrid;                 //DungeonRoot/Grid (���� �׸���)
    public Transform roomsRoot;              //DungeonRoot/RoomsRoot
    public GameObject startRoomPrefab;       //���� �� ������
    public List<GameObject> candidateRooms;  //�߰��� �� �����յ�
    public RoomCorridorPainter painter;      //���� �����ÿ�

    private readonly List<PlacedRoom> placed = new();

    private readonly int attachCount = 4;  //���� �� �ܿ� �߰��� �� ����
    private readonly int hallMin = 5;      //���� �ּ� ����(Ÿ��)
    private readonly int hallMax = 10;     //���� �ִ� ����(Ÿ��)

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        //�ʱ�ȭ
        ClearChildren(roomsRoot);
        placed.Clear();

        //1) (���� �׸��� ������) ���� �� ��ġ
        var start = PlaceRoomAt(startRoomPrefab, Vector3Int.zero);
        placed.Add(start);

        //2) ���� ������ŭ �̾���̱�
        for (int i = 0; i < attachCount; i++)
        {
            var nextPrefab = candidateRooms[Random.Range(0, candidateRooms.Count)];
            if (!TryAttachRoom(nextPrefab))
            {
                Debug.Log("[RoomPlacer] ���� ȣȯ ��� ����. ���� �õ�.");
            }
        }
    }

    //������ '�̻�� ��'�� �� ���� 'ȣȯ�Ǵ� ��'�� ã�� ��ġ
    private bool TryAttachRoom(GameObject newRoomPrefab)
    {
        foreach (var a in AllFreeDoors())
        {
            var preview = Instantiate(newRoomPrefab);
            var newAnchors = preview.GetComponentsInChildren<DoorAnchor>(true);

            //�� ���� + �ݴ� ���⸸ �ĺ�
            var candidates = newAnchors.Where(b =>
                b.width == a.anchor.width &&
                DoorAnchor.Opposite(a.anchor.direction) == b.direction);

            foreach (var b in candidates)
            {
                var bWorldCenter = b.GetWorldCenter();
                var bLocalCell = dungeonGrid.WorldToCell(bWorldCenter);
                var origin = a.worldTile - (Vector3Int)new Vector2Int(bLocalCell.x, bLocalCell.y);

                //���� ���̸�ŭ, ��� �� �������� ���� �� �о��
                int L = Random.Range(hallMin, hallMax + 1);
                origin += DirToVec(a.anchor.direction) * L;

                Destroy(preview);
                var placed = PlaceRoomAt(newRoomPrefab, origin);

                var placedB = FindMatchingDoor(placed, b);
                if (placedB == null)
                {
                    Debug.Log("[RoomPlacer] �� �濡�� ��Ī�Ǵ� ���� ã�� ����");
                    Destroy(placed.go);
                    return false;
                }

                //������ '�´��'�� �ƴ϶� 'Lĭ ������ ��ġ'�� ����
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

    //�� ���� origin�� ��ġ�ϰ� DoorAnchor���� ��ĵ�Ͽ� worldTile�� ���
    private PlacedRoom PlaceRoomAt(GameObject prefab, Vector3Int origin)
    {
        var inst = Instantiate(prefab, roomsRoot);
        inst.name = prefab.name;
        inst.transform.position = dungeonGrid.CellToWorld(origin);

        var pr = new PlacedRoom { go = inst, origin = origin };
        RebuildPlacedDoors(pr);
        return pr;
    }

    //��ġ�� ���� DoorAnchor��� PlacedDoor ����� �ٽ� ����
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

    //��/���� ��ġ + ���� ����� ���� ���� (���� ������ ������ �����Ǵ� DoorAnchor�� ã�� ����)
    private PlacedDoor FindMatchingDoor(PlacedRoom placed, DoorAnchor srcAnchor)
    {
        var cands = placed.doors.Where(d => d.anchor.width == srcAnchor.width &&
                                            d.anchor.direction == srcAnchor.direction).ToList();
        if (cands.Count == 0) return null;

        //���� ����� �� ���� (��κ� �� ���� ����)
        cands.Sort((x, y) => (x.worldTile - placed.origin).sqrMagnitude
                           .CompareTo((y.worldTile - placed.origin).sqrMagnitude));
        return cands[0];
    }

    //DoorDir �� Ÿ�� ��ǥ ���� ���� ����
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
}