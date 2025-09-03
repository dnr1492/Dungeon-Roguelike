using Unity.Collections;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class DoorAnchor : MonoBehaviour
{
    [Tooltip("문이 뚫린 벽의 방향")] public DoorDir direction = DoorDir.North;
    [Tooltip("문 폭(타일 수). 홀수(1/3/5…)")] [Min(1)] public int width = 1;

    private readonly bool autoSnapToGrid = true;
    private Grid parentGrid;
    [SerializeField, ReadOnly] private Vector2Int tileOffset;

    //에디터 모드에서 위치를 옮길 때 캐시 최신화
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

    //부모에서 Grid 참조 보장
    private void EnsureRefs()
    {
        if (!parentGrid) parentGrid = GetComponentInParent<Grid>();
    }

    //Grid 스냅 + 타일 좌표 캐싱
    private void SnapAndCache()
    {
        if (!parentGrid) return;

        //월드 → 셀 → 셀 센터로 스냅
        var cell = parentGrid.WorldToCell(transform.position);
        var center = parentGrid.GetCellCenterWorld(cell);

        if (autoSnapToGrid)
            transform.position = center;

        tileOffset = new Vector2Int(cell.x, cell.y);
    }

    //홀수 폭 보정
    private void ClampWidthOdd()
    {
        if (width < 1) width = 1;
        //짝수를 강제로 홀수로 보정
        //짝수라면: (width & 1) == 0 
        if ((width & 1) == 0) width += 1;
    }

    /// <summary>
    /// 문 중앙(앵커)을 기준으로, 문이 차지하는 모든 타일 좌표(로컬 타일 좌표)를 반환
    /// width = 5이면 -2..+2 오프셋으로 계산
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
    /// 문 중앙 타일의 '세계 좌표' (셀 센터)를 반환
    /// 런타임 페인터/연결 계산에서 유용
    /// </summary>
    public Vector3 GetWorldCenter()
    {
        if (!parentGrid) return transform.position;
        var cell = new Vector3Int(tileOffset.x, tileOffset.y, 0);
        return parentGrid.GetCellCenterWorld(cell);
    }

    /// <summary>
    /// 반대 방향을 반환 (문 매칭에 사용)
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

    #region Gizmo 시각화
#if UNITY_EDITOR
    [Header("Gizmo")]
    public Color gizmoColor = new Color(1f, 0.25f, 0.25f, 0.8f);
    public float gizmoDepth = 1;  //문 폭 표시 두께

    private void OnDrawGizmosSelected()
    {
        EnsureRefs();

        //문 타일 범위를 화면에 표기
        var tiles = GetDoorTiles();
        Gizmos.color = gizmoColor;

        if (parentGrid)
        {
            foreach (var t in tiles)
            {
                var center = parentGrid.GetCellCenterWorld(new Vector3Int(t.x, t.y, 0));
                var size = parentGrid.cellSize;
                //살짝 얇게 그려서 겹침 구분
                var drawSize = new Vector3(
                    direction == DoorDir.North || direction == DoorDir.South ? size.x : gizmoDepth,
                    direction == DoorDir.East || direction == DoorDir.West ? size.y : gizmoDepth,
                    0.0f
                );
                Gizmos.DrawCube(center, drawSize);
            }
        }

        //중앙 방향 화살표
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
