using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public struct ThemeSet
{
    public ThemeId theme;                    //테마 식별 (Desert, Ice, ...)
    public RoomPropPattern[] combatDefault;  //CombatRoom 공통 패턴 묶음
    public RoomPropPattern[] eliteDefault;   //EliteRoom 공통 패턴 묶음
    public ProfileOverride[] overrides;      //프로필(Variant 등)별 특수 패턴
}

[System.Serializable]
public struct ProfileOverride
{
    public RoomDecorProfile profile;            //이 프로필일 때
    public RoomPropPattern[] combat;            //CombatRoom에 쓸 패턴
    public RoomPropPattern[] elite;             //EliteRoom에 쓸 패턴
    public Vector2Int placeCountRangeOverride;  //해당 방에 페인팅할 장식 최소/최대 개수: (0,0)일 경우 무시하고 ThemeSet의 기본값을 사용
}

public class RoomPropPainter : MonoBehaviour
{
    private RoomPropPattern[] patterns;
    private readonly HashSet<Vector3Int> reserved = new();

    [Header("Tile")]
    [SerializeField] Tilemap baseMap;
    [SerializeField] Tilemap wallsMap;
    [SerializeField] Tilemap collisionMap;
    [SerializeField] Tilemap propsMap;
    [SerializeField] TileBase collisionTile;

    [Header("Rule")]
    [SerializeField] Vector2Int placeCountRange = new Vector2Int(6, 14);  //방 하나에 페인팅할 장식 최소/최대 개수
    private int minDistanceFromDoor = 2;                                  //문 주변 N칸 이내 장식 금지
    private int minDistanceFromWall = 1;                                  //벽 주변 N칸 이내 장식 금지
    private bool avoidBaseHoles = true;                                   //바닥(타일) 없는 칸 금지
    private int avoidNearCollisionRadius = 1;                             //기존 충돌 타일 주변 N칸 금지
    private int minDistanceBetweenProps = 1;                              //장식 주변 N칸 이내 장식 금지

    [Header("Per-Room Profile")]
    [SerializeField] RoomDecorProfile profile = RoomDecorProfile.Default;   //이 방 프리팹의 장식 프로필
    [SerializeField] Vector2Int placeCountRangeOverride = Vector2Int.zero;  //이 방 전용 페인팅할 장식 최소/최대 개수: (0,0)일 경우 무시하고 ThemeSet의 기본값을 사용

    public void ApplyThemeSet(ThemeSet ts, string roomTag)
    {
        RoomPropPattern[] picked = null;
        Vector2Int countOv = Vector2Int.zero;

        if (ts.overrides != null)
        {
            foreach (var o in ts.overrides)
            {
                if (o.profile != profile) continue;
                picked = PickByTag(roomTag, o.combat, o.elite);
                if (picked != null && picked.Length > 0)
                {
                    countOv = o.placeCountRangeOverride;
                    break;
                }
            }
        }
        if (picked == null || picked.Length == 0)
            picked = PickByTag(roomTag, ts.combatDefault, ts.eliteDefault);

        patterns = picked;

        if (placeCountRangeOverride != Vector2Int.zero) SetPlaceCountRange(placeCountRangeOverride);
        else if (countOv != Vector2Int.zero) SetPlaceCountRange(countOv);
    }

    private void SetPlaceCountRange(Vector2Int range)
    {
        if (range.x < 0 || range.y < 0) return;
        if (range.x > range.y) (range.x, range.y) = (range.y, range.x);
        placeCountRange = range;
    }

    public void PaintDeterministic(System.Random rng, List<Vector3Int> doorCentersWorld, Grid dungeonGrid)
    {
        if (!propsMap || !baseMap) return;
        if (patterns == null || patterns.Length == 0) return;
        reserved.Clear();

        var candidates = CollectCandidateCells(dungeonGrid, doorCentersWorld);
        int want = Mathf.Clamp(rng.Next(placeCountRange.x, placeCountRange.y + 1), 0, candidates.Count);
        Shuffle(rng, candidates);

        int placed = 0;
        foreach (var anchor in candidates)
        {
            if (placed >= want) break;
            if (TryPlaceRandomPatternAt(rng, anchor))
                placed++;
        }

        //타일 갱신
        propsMap.RefreshAllTiles();
        collisionMap.RefreshAllTiles();

        //콜라이더 자동 재생성
        //Manual 설정에서도 런타임 자동 갱신
        var tmc = collisionMap.GetComponent<TilemapCollider2D>();
        if (tmc) tmc.ProcessTilemapChanges();
        var comp = collisionMap ? collisionMap.GetComponent<CompositeCollider2D>() : null;
        if (comp != null) comp.GenerateGeometry();
        Physics2D.SyncTransforms();
    }

    private List<Vector3Int> CollectCandidateCells(Grid dungeonGrid, List<Vector3Int> doorCentersWorld)
    {
        var result = new List<Vector3Int>();
        var bounds = baseMap.cellBounds;

        //문 주변 금지 영역
        var forbid = new HashSet<Vector3Int>();
        foreach (var wc in doorCentersWorld)
        {
            var local = baseMap.WorldToCell(dungeonGrid.CellToWorld(wc));
            for (int dx = -minDistanceFromDoor; dx <= minDistanceFromDoor; dx++)
                for (int dy = -minDistanceFromDoor; dy <= minDistanceFromDoor; dy++)
                    forbid.Add(new Vector3Int(local.x + dx, local.y + dy, 0));
        }

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                var p = new Vector3Int(x, y, 0);
                if (avoidBaseHoles && !baseMap.HasTile(p)) continue;
                if (forbid.Contains(p)) continue;
                if (IsNearWall(p, minDistanceFromWall)) continue;
                if (HasCollisionInRadius(p, avoidNearCollisionRadius)) continue;
                if (propsMap.HasTile(p)) continue;
                result.Add(p);
            }
        }
            
        return result;
    }

    private bool TryPlaceRandomPatternAt(System.Random rng, Vector3Int anchor)
    {
        if (patterns == null || patterns.Length == 0) return false;

        //확률 가중치 선택
        int total = 0;
        foreach (var p in patterns) if (p) total += Mathf.Max(1, p.weight);
        if (total <= 0) return false;

        int pick = rng.Next(total);
        RoomPropPattern pat = null;
        foreach (var p in patterns)
        {
            int w = Mathf.Max(1, p.weight);
            if (pick < w) { pat = p; break; }
            pick -= w;
        }
        if (pat == null || pat.cells == null || pat.cells.Length == 0) return false;

        int rot = pat.allowRotate90 ? rng.Next(0, 4) : 0;
        bool mx = pat.allowMirrorX && rng.Next(0, 2) == 1;
        bool my = pat.allowMirrorY && rng.Next(0, 2) == 1;

        var targets = new List<(Vector3Int cell, RoomPropPattern.Cell def)>(pat.cells.Length);
        foreach (var c in pat.cells)
        {
            var o = c.offset;
            if (mx) o = new Vector2Int(-o.x, o.y);
            if (my) o = new Vector2Int(o.x, -o.y);
            for (int i = 0; i < rot; i++) o = new Vector2Int(-o.y, o.x);
            var dest = anchor + new Vector3Int(o.x, o.y, 0);
            targets.Add((dest, c));
        }

        foreach (var (cell, def) in targets)
        {
            if (avoidBaseHoles && !baseMap.HasTile(cell)) return false;
            if (propsMap.HasTile(cell)) return false;
            if (reserved.Contains(cell)) return false;
            if (IsNearWall(cell, minDistanceFromWall)) return false;
            if (HasCollisionInRadius(cell, avoidNearCollisionRadius)) return false;
            if (collisionMap && collisionMap.HasTile(cell)) return false;
            if (HasReservedInRadius(cell, minDistanceBetweenProps)) return false;
        }

        foreach (var (cell, def) in targets)
        {
            if (def.tile) propsMap.SetTile(cell, def.tile);

            var pivot = Vector3.zero;
            var S = new Vector3(mx ? -1f : 1f, my ? -1f : 1f, 1f);
            float angle = rot * 90f;

            var M =
                Matrix4x4.TRS(pivot, Quaternion.Euler(0, 0, angle), Vector3.one) *
                Matrix4x4.Scale(S);

            propsMap.SetTransformMatrix(cell, M);

            if (def.obstacle && collisionMap)
                collisionMap.SetTile(cell, collisionTile);

            reserved.Add(cell);
        }

        return true;
    }

    private RoomPropPattern[] PickByTag(string tag, RoomPropPattern[] combat, RoomPropPattern[] elite)
    {
        if (tag == ConstClass.Tags.CombatRoom) return combat;
        if (tag == ConstClass.Tags.EliteRoom) return elite;
        return null;
    }

    private bool IsNearWall(Vector3Int cell, int minDist)
    {
        if (!wallsMap || minDist <= 0) return false;
        for (int dx = -minDist; dx <= minDist; dx++)
        {
            for (int dy = -minDist; dy <= minDist; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (wallsMap.HasTile(new Vector3Int(cell.x + dx, cell.y + dy, 0))) return true;
            }
        }
        return false;
    }

    private bool HasCollisionInRadius(Vector3Int cell, int r)
    {
        if (!collisionMap || r <= 0) return false;
        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (collisionMap.HasTile(new Vector3Int(cell.x + dx, cell.y + dy, 0))) return true;
            }
        }
        return false;
    }

    private void Shuffle<T>(System.Random rng, List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private bool HasReservedInRadius(Vector3Int cell, int r)
    {
        if (r <= 0 || reserved.Count == 0) return false;
        for (int dx = -r; dx <= r; dx++)
        {
            for (int dy = -r; dy <= r; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (reserved.Contains(new Vector3Int(cell.x + dx, cell.y + dy, 0)))
                    return true;
            }
        }
        return false;
    }
}