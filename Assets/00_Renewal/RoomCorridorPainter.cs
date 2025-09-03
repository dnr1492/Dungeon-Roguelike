using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomCorridorPainter : MonoBehaviour
{
    public Tilemap baseMap;         //복도 바닥
    public Tilemap wallMap;         //복도 벽
    public Tilemap collisionMap;    //충돌 전용

    public TileBase floorTile;      //바닥 타일
    public TileBase wallTile;       //벽 타일
    public TileBase collisionTile;  //충돌 타일

    //출발 문 방향을 받아 가로/세로 순서를 고정
    public void PaintCorridor(Vector2Int from, Vector2Int to, int width, DoorDir startDir)
    {
        //직선이면 해당 축만
        if (from.x == to.x)
        {
            PaintVerticalSegment(from, to, width);
            Finish();
            return;
        }
        if (from.y == to.y)
        {
            PaintHorizontalSegment(from, to, width);
            Finish();
            return;
        }

        //규칙 확정 (North/South → 가로 먼저, East/West → 세로 먼저)
        bool horizontalFirst = (startDir == DoorDir.North || startDir == DoorDir.South);

        Vector2Int turn = horizontalFirst
            ? new Vector2Int(to.x, from.y)   //가로 먼저
            : new Vector2Int(from.x, to.y);  //세로 먼저

        if (horizontalFirst)
        {
            PaintHorizontalSegment(from, turn, width);
            PaintVerticalSegment(turn, to, width);
        }
        else
        {
            PaintVerticalSegment(from, turn, width);
            PaintHorizontalSegment(turn, to, width);
        }

        CornerReinforce(turn, width);

        Finish();
    }

    //가로 구간: x만 이동, y는 고정. 바닥 폭을 세로 방향으로 확장하고 양측 '벽 + 충돌' 생성
    private void PaintHorizontalSegment(Vector2Int a, Vector2Int b, int width)
    {
        int half = width / 2;
        int step = a.x <= b.x ? 1 : -1;

        //양 끝 포함
        for (int x = a.x; x != b.x + step; x += step) 
        {
            //바닥 (통행 가능)
            for (int o = -half; o <= half; o++)
            {
                var cell = new Vector2Int(x, a.y + o);
                baseMap.SetTile((Vector3Int)cell, floorTile);
                collisionMap.SetTile((Vector3Int)cell, null);
            }

            //양측 벽 + 충돌
            var top = new Vector2Int(x, a.y + (half + 1));
            var bot = new Vector2Int(x, a.y - (half + 1));
            PlaceWall(top);
            PlaceWall(bot);
        }
    }

    //세로 구간: y만 이동, x는 고정. 바닥 폭을 가로 방향으로 확장하고 양측 '벽 + 충돌' 생성
    private void PaintVerticalSegment(Vector2Int a, Vector2Int b, int width)
    {
        int half = width / 2;
        int step = a.y <= b.y ? 1 : -1;

        //양 끝 포함
        for (int y = a.y; y != b.y + step; y += step) 
        {
            //바닥 (통행 가능)
            for (int o = -half; o <= half; o++)
            {
                var cell = new Vector2Int(a.x + o, y);
                baseMap.SetTile((Vector3Int)cell, floorTile);
                collisionMap.SetTile((Vector3Int)cell, null);
            }

            //양측 벽 + 충돌
            var right = new Vector2Int(a.x + (half + 1), y);
            var left = new Vector2Int(a.x - (half + 1), y);
            PlaceWall(right);
            PlaceWall(left);
        }
    }

    //ㄱ자 코너의 외곽 모서리를 보강 (틈 방지)
    private void CornerReinforce(Vector2Int turn, int width)
    {
        int half = width / 2;
        PlaceWall(new Vector2Int(turn.x + (half + 1), turn.y + (half + 1)));
        PlaceWall(new Vector2Int(turn.x + (half + 1), turn.y - (half + 1)));
        PlaceWall(new Vector2Int(turn.x - (half + 1), turn.y + (half + 1)));
        PlaceWall(new Vector2Int(turn.x - (half + 1), turn.y - (half + 1)));
    }

    //벽 타일을 찍고, 동일 좌표에 충돌 타일도 생성
    private void PlaceWall(Vector2Int pos)
    {
        if (wallMap && wallTile)
            wallMap.SetTile((Vector3Int)pos, wallTile);

        if (collisionMap && collisionTile)
            collisionMap.SetTile((Vector3Int)pos, collisionTile);
    }

    //타일 갱신 + 콜라이더 자동 재생성
    private void Finish()
    {
        if (baseMap) baseMap.RefreshAllTiles();
        if (wallMap) wallMap.RefreshAllTiles();
        if (collisionMap) collisionMap.RefreshAllTiles();

        //Manual 설정에서도 런타임 자동 갱신
        var tmc = collisionMap ? collisionMap.GetComponent<TilemapCollider2D>() : null;
        if (tmc != null) tmc.ProcessTilemapChanges();
        var comp = collisionMap ? collisionMap.GetComponent<CompositeCollider2D>() : null;
        if (comp != null) comp.GenerateGeometry();
        Physics2D.SyncTransforms();
    }
}