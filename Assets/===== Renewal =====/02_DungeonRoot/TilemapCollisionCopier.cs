#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class TilemapCollisionCopier : MonoBehaviour
{
    [Header("Sources")]
    public Tilemap walls;           //Walls 타일맵
    public Tilemap props;           //Props 타일맵

    [Header("Target")]
    public Tilemap collision;       //Collision 타일맵
    public TileBase collisionTile;  //충돌 전용 타일 (스프라이트 없어도 됨, ColliderType = Grid)

    [Header("Rule")]
    private readonly string obstacleSuffix = "_OB";  //스프라이트 이름 규칙 (예: crate_OB.png)

    [ContextMenu("Copy Walls + Props(BySpriteName) → Collision")]
    private void Copy()
    {
        if (!collision || !collisionTile)
        {
            Debug.Log("[TilemapCopier] collision/collisionTile 미지정");
            return;
        }

        collision.ClearAllTiles();

        int wallsCnt = 0, propsCnt = 0;

        //1) Walls 전부 복사
        if (walls) wallsCnt = CopyAll(walls);

        //2) Props 중 *_OB 스프라이트만 복사
        if (props) propsCnt = CopyObstacles(props, obstacleSuffix);

        collision.RefreshAllTiles();

        var tmc = collision.GetComponent<TilemapCollider2D>();
        if (tmc) tmc.ProcessTilemapChanges();

        Debug.Log($"[Copier] Walls = {wallsCnt}, Props(OB) = {propsCnt}");
    }

    private int CopyAll(Tilemap src)
    {
        int placed = 0;
        foreach (var pos in src.cellBounds.allPositionsWithin)
        {
            var t = src.GetTile(pos);
            if (t == null) continue;

            collision.SetTile(pos, collisionTile);
            placed++;
        }
        return placed;
    }

    private int CopyObstacles(Tilemap src, string suffix)
    {
        int placed = 0;
        foreach (var pos in src.cellBounds.allPositionsWithin)
        {
            var t = src.GetTile(pos);
            if (t is Tile tile)
            {
                var spriteName = tile.sprite != null ? tile.sprite.name : "";
                if (spriteName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    collision.SetTile(pos, collisionTile);
                    placed++;
                }
            }
        }
        return placed;
    }
}
#endif