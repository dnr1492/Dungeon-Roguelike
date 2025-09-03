using Unity.Collections;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class DoorAnchor : MonoBehaviour
{
    [Tooltip("���� �ո� ���� ����")] public DoorDir direction = DoorDir.North;
    [Tooltip("�� ��(Ÿ�� ��). Ȧ��(1/3/5��)")] [Min(1)] public int width = 1;

    private readonly bool autoSnapToGrid = true;
    private Grid parentGrid;
    [SerializeField, ReadOnly] private Vector2Int tileOffset;

    //������ ��忡�� ��ġ�� �ű� �� ĳ�� �ֽ�ȭ
    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && autoSnapToGrid)
        {
            SnapAndCache();
        }
#endif
    }

    private void OnEnable()
    {
        EnsureRefs();
        SnapAndCache();
    }

    private void OnValidate()
    {
        EnsureRefs();
        SnapAndCache();
        ClampWidthOdd();
    }

    //�θ𿡼� Grid ���� ����
    private void EnsureRefs()
    {
        if (!parentGrid) parentGrid = GetComponentInParent<Grid>();
    }

    //Grid ���� + Ÿ�� ��ǥ ĳ��
    private void SnapAndCache()
    {
        if (!parentGrid) return;

        //���� �� �� �� �� ���ͷ� ����
        var cell = parentGrid.WorldToCell(transform.position);
        var center = parentGrid.GetCellCenterWorld(cell);

        if (autoSnapToGrid)
            transform.position = center;

        tileOffset = new Vector2Int(cell.x, cell.y);
    }

    //Ȧ�� �� ����
    private void ClampWidthOdd()
    {
        if (width < 1) width = 1;
        //¦���� ������ Ȧ���� ����
        //¦�����: (width & 1) == 0 
        if ((width & 1) == 0) width += 1;
    }

    /// <summary>
    /// �� �߾�(��Ŀ)�� ��������, ���� �����ϴ� ��� Ÿ�� ��ǥ(���� Ÿ�� ��ǥ)�� ��ȯ
    /// width = 5�̸� -2..+2 ���������� ���
    /// </summary>
    public Vector2Int[] GetDoorTiles()
    {
        int half = width / 2;
        var list = new Vector2Int[width];

        for (int i = 0; i < width; i++)
        {
            int o = i - half;
            switch (direction)
            {
                case DoorDir.North:
                case DoorDir.South:
                    list[i] = new Vector2Int(tileOffset.x + o, tileOffset.y);
                    break;
                case DoorDir.East:
                case DoorDir.West:
                    list[i] = new Vector2Int(tileOffset.x, tileOffset.y + o);
                    break;
            }
        }
        return list;
    }

    /// <summary>
    /// �� �߾� Ÿ���� '���� ��ǥ' (�� ����)�� ��ȯ
    /// ��Ÿ�� ������/���� ��꿡�� ����
    /// </summary>
    public Vector3 GetWorldCenter()
    {
        if (!parentGrid) return transform.position;
        var cell = new Vector3Int(tileOffset.x, tileOffset.y, 0);
        return parentGrid.GetCellCenterWorld(cell);
    }

    /// <summary>
    /// �ݴ� ������ ��ȯ (�� ��Ī�� ���)
    /// </summary>
    public static DoorDir Opposite(DoorDir d)
    {
        switch (d)
        {
            case DoorDir.North: return DoorDir.South;
            case DoorDir.South: return DoorDir.North;
            case DoorDir.East: return DoorDir.West;
            case DoorDir.West: return DoorDir.East;
        }
        return d;
    }

    #region Gizmo �ð�ȭ
#if UNITY_EDITOR
    [Header("Gizmo")]
    public Color gizmoColor = new Color(1f, 0.25f, 0.25f, 0.8f);
    public float gizmoDepth = 1;  //�� �� ǥ�� �β�

    private void OnDrawGizmosSelected()
    {
        EnsureRefs();

        //�� Ÿ�� ������ ȭ�鿡 ǥ��
        var tiles = GetDoorTiles();
        Gizmos.color = gizmoColor;

        if (parentGrid)
        {
            foreach (var t in tiles)
            {
                var center = parentGrid.GetCellCenterWorld(new Vector3Int(t.x, t.y, 0));
                var size = parentGrid.cellSize;
                //��¦ ��� �׷��� ��ħ ����
                var drawSize = new Vector3(
                    direction == DoorDir.North || direction == DoorDir.South ? size.x : gizmoDepth,
                    direction == DoorDir.East || direction == DoorDir.West ? size.y : gizmoDepth,
                    0.0f
                );
                Gizmos.DrawCube(center, drawSize);
            }
        }

        //�߾� ���� ȭ��ǥ
        var c = GetWorldCenter();
        Vector3 dir =
            direction == DoorDir.North ? Vector3.up :
            direction == DoorDir.South ? Vector3.down :
            direction == DoorDir.East ? Vector3.right : Vector3.left;

        Gizmos.DrawRay(c, dir * 0.5f);
    }
#endif
    #endregion
}
