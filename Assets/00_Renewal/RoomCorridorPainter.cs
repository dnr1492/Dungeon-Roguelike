using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomCorridorPainter : MonoBehaviour
{
    [SerializeField] Tilemap baseMap;         //복도 바닥
    [SerializeField] Tilemap wallMap;         //복도 벽
    [SerializeField] Tilemap collisionMap;    //충돌 전용

    [SerializeField] TileBase floorTile;      //바닥 타일
    [SerializeField] TileBase wallTile;       //벽 타일
    [SerializeField] TileBase collisionTile;  //충돌 타일

    //방향별 벽 타일(미지정 시 기본 wallTile 사용)
    [SerializeField] TileBase wallTileUp;
    [SerializeField] TileBase wallTileDown;
    [SerializeField] TileBase wallTileLeft;
    [SerializeField] TileBase wallTileRight;

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

        Finish();
    }

    //가로 구간 (좌 ↔ 우)
    //바닥 동일
    //'상/하' 벽 생성
    private void PaintHorizontalSegment(Vector2Int a, Vector2Int b, int width)
    {
        int half = width / 2;
        int step = a.x <= b.x ? 1 : -1;

        for (int x = a.x; x != b.x + step; x += step)
        {
            //바닥 (통행 가능)
            for (int o = -half; o <= half; o++)
            {
                var cell = new Vector2Int(x, a.y + o);
                baseMap.SetTile((Vector3Int)cell, floorTile);
                collisionMap.SetTile((Vector3Int)cell, null);
            }

            //문 시작/끝 지점 제외 벽 생성
            bool isEndColumn = (x == a.x) || (x == b.x);
            if (!isEndColumn)
            {
                var top = new Vector2Int(x, a.y + (half + 1));
                var bot = new Vector2Int(x, a.y - (half + 1));
                PlaceWallDirectional(top, "Up");
                PlaceWallDirectional(bot, "Down");
            }
        }
    }

    //세로 구간 (상 ↔ 하)
    //바닥 동일
    //'좌/우' 벽 생성
    private void PaintVerticalSegment(Vector2Int a, Vector2Int b, int width)
    {
        int half = width / 2;
        int step = a.y <= b.y ? 1 : -1;

        for (int y = a.y; y != b.y + step; y += step)
        {
            //바닥 (통행 가능)
            for (int o = -half; o <= half; o++)
            {
                var cell = new Vector2Int(a.x + o, y);
                baseMap.SetTile((Vector3Int)cell, floorTile);
                collisionMap.SetTile((Vector3Int)cell, null);
            }

            //문 시작/끝 지점 제외 벽 생성
            bool isEndRow = (y == a.y) || (y == b.y);
            if (!isEndRow)
            {
                var right = new Vector2Int(a.x + (half + 1), y);
                var left = new Vector2Int(a.x - (half + 1), y);
                PlaceWallDirectional(right, "Right");
                PlaceWallDirectional(left, "Left");
            }
        }
    }

    //방향별로 벽/충돌을 찍는 헬퍼
    private void PlaceWallDirectional(Vector2Int pos, string dir)
    {
        TileBase use = wallTile;
        if (dir == "Up" && wallTileUp) use = wallTileUp;
        if (dir == "Down" && wallTileDown) use = wallTileDown;
        if (dir == "Left" && wallTileLeft) use = wallTileLeft;
        if (dir == "Right" && wallTileRight) use = wallTileRight;

        if (wallMap && use) wallMap.SetTile((Vector3Int)pos, use);
        if (collisionMap && collisionTile) collisionMap.SetTile((Vector3Int)pos, collisionTile);
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

    //방 입구 게이트 (락/언락)
    public void SetDoorGate(Vector2Int doorCenter, DoorDir dir, int width, bool closed)
    {
        var line = GateLineCells(doorCenter, dir, width);
        foreach (var cell in line)
        {
            if (closed)
            {
                //닫힘: 벽 + 충돌 생성, 바닥 제거
                if (wallMap && wallTile) wallMap.SetTile(cell, wallTile);
                if (collisionMap && collisionTile) collisionMap.SetTile(cell, collisionTile);
                if (baseMap) baseMap.SetTile(cell, null);
            }
            else
            {
                //열림: 바닥 복구, 벽 + 충돌 제거
                if (baseMap && floorTile) baseMap.SetTile(cell, floorTile);
                if (wallMap) wallMap.SetTile(cell, null);
                if (collisionMap) collisionMap.SetTile(cell, null);
            }
        }

        Finish();
    }

    //문에 놓일 게이트 1줄 좌표를 계산
    private IEnumerable<Vector3Int> GateLineCells(Vector2Int doorCenter, DoorDir dir, int width)
    {
        int half = width / 2;

        if (dir == DoorDir.North || dir == DoorDir.South)
        {
            int y = doorCenter.y;
            for (int o = -half; o <= half; o++)
                yield return new Vector3Int(doorCenter.x + o, y, 0);
        }
        else
        {
            int x = doorCenter.x;
            for (int o = -half; o <= half; o++)
                yield return new Vector3Int(x, doorCenter.y + o, 0);
        }
    }
}