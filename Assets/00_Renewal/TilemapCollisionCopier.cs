#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class TilemapCollisionCopier : MonoBehaviour
{
    [Header("Sources")]
    public Tilemap walls;           //Walls Ÿ�ϸ�
    public Tilemap props;           //Props Ÿ�ϸ�

    [Header("Target")]
    public Tilemap collision;       //Collision Ÿ�ϸ�
    public TileBase collisionTile;  //�浹 ���� Ÿ�� (��������Ʈ ��� ��, ColliderType = Grid)

    [Header("Rule")]
    private readonly string obstacleSuffix = "_OB";  //��������Ʈ �̸� ��Ģ (��: crate_OB.png)

    [ContextMenu("Copy Walls + Props(BySpriteName) �� Collision")]
    private void Copy()
    {
        if (!collision || !collisionTile)
        {
            Debug.Log("[TilemapCopier] collision/collisionTile ������");
            return;
        }

        collision.ClearAllTiles();

        int wallsCnt = 0, propsCnt = 0;

        //1) Walls ���� ����
        if (walls) wallsCnt = CopyAll(walls);

        //2) Props �� *_OB ��������Ʈ�� ����
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