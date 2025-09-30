using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoomCorridorPainter : MonoBehaviour
{
    [SerializeField] Tilemap baseMap;         //복도 바닥
    [SerializeField] Tilemap wallMap;         //복도 벽
    [SerializeField] Tilemap collisionMap;    //충돌 전용

    [SerializeField] TileBase floorTile;      //바닥 타일
    [SerializeField] TileBase doorWallTile;   //문 벽 타일
    [SerializeField] TileBase collisionTile;  //충돌 타일

    //방향별 벽 타일
    [SerializeField] TileBase wallTileUp;
    [SerializeField] TileBase wallTileDown;
    [SerializeField] TileBase wallTileLeft;
    [SerializeField] TileBase wallTileRight;

    //출발 문 방향을 받아 가로/세로 순서를 고정
    public void PaintCorridor(Vector2Int from, Vector2Int to, int width)
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
        int step = a.x <= b.x ? 1 : -1;
        int half = width / 2;
        int maxFloorOffset = ((width & 1) == 1) ? half : (half - 1);  //윗변(상단) 바닥의 최댓값
        int minFloorOffset = -half;                                   //아랫변(하단) 바닥의 최솟값
        int topOffset = maxFloorOffset + 1;                           //상단 벽은 바닥 위로 1칸
        int botOffset = minFloorOffset - 1;                           //하단 벽은 바닥 아래로 1칸

        for (int x = a.x; x != b.x + step; x += step)
        {
            //바닥 (통행 가능)
            foreach (int o in Offsets(width))
            {
                var cell = new Vector2Int(x, a.y + o);
                if (baseMap && floorTile) baseMap.SetTile((Vector3Int)cell, floorTile);
                if (collisionMap) collisionMap.SetTile((Vector3Int)cell, null);
            }

            //문 시작/끝 지점 제외 벽 생성
            bool isEndColumn = (x == a.x) || (x == b.x);
            if (!isEndColumn)
            {
                PlaceWallDirectional(new Vector2Int(x, a.y + topOffset), "Up");
                PlaceWallDirectional(new Vector2Int(x, a.y + botOffset), "Down");
            }
        }
    }

    //세로 구간 (상 ↔ 하)
    //바닥 동일
    //'좌/우' 벽 생성
    private void PaintVerticalSegment(Vector2Int a, Vector2Int b, int width)
    {
        int step = a.y <= b.y ? 1 : -1;
        int half = width / 2;
        int maxFloorOffset = ((width & 1) == 1) ? half : (half - 1);  //오른쪽(우측) 바닥의 최댓값
        int minFloorOffset = -half;                                   //왼쪽(좌측) 바닥의 최솟값
        int rightOffset = maxFloorOffset + 1;                         //우측 벽은 바닥 바깥 1칸
        int leftOffset = minFloorOffset - 1;                          //좌측 벽은 바닥 바깥 1칸

        for (int y = a.y; y != b.y + step; y += step)
        {
            //바닥 (통행 가능)
            foreach (int o in Offsets(width))
            {
                var cell = new Vector2Int(a.x + o, y);
                if (baseMap && floorTile) baseMap.SetTile((Vector3Int)cell, floorTile);
                if (collisionMap) collisionMap.SetTile((Vector3Int)cell, null);
            }

            //문 시작/끝 지점 제외 벽 생성
            bool isEndRow = (y == a.y) || (y == b.y);
            if (!isEndRow)
            {
                PlaceWallDirectional(new Vector2Int(a.x + rightOffset, y), "Right");
                PlaceWallDirectional(new Vector2Int(a.x + leftOffset, y), "Left");
            }
        }
    }

    //방향별로 벽/충돌을 찍는 헬퍼
    private void PlaceWallDirectional(Vector2Int pos, string dir)
    {
        TileBase use = null;

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

    //폭을 홀수/짝수 모두 정확히 width개 생성
    private IEnumerable<int> Offsets(int width)
    {
        int half = width / 2;
        if ((width & 1) == 1) {
            for (int o = -half; o <= half; o++) yield return o;
        }
        else {
            for (int o = -half; o < half; o++) yield return o;
        }
    }

    //방 입구 게이트 (락/언락)
    public void SetDoorGate(Vector2Int doorCenter, DoorDir dir, int width, bool closed)
    {
        var line = GateLineCells(doorCenter, dir, width);
        foreach (var cell in line)
        {
            if (closed)
            {
                if (wallMap)
                {
                    var use = doorWallTile != null ? doorWallTile : (dir == DoorDir.North ? wallTileUp :
                                           dir == DoorDir.South ? wallTileDown :
                                           dir == DoorDir.East ? wallTileRight :
                                                                  wallTileLeft);
                    if (use) wallMap.SetTile(cell, use);
                }
                if (collisionMap && collisionTile) collisionMap.SetTile(cell, collisionTile);
                if (baseMap) baseMap.SetTile(cell, null);
            }
            else
            {
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
        if (dir == DoorDir.North || dir == DoorDir.South)
        {
            int y = doorCenter.y;
            foreach (int o in Offsets(width))
                yield return new Vector3Int(doorCenter.x + o, y, 0);
        }
        else
        {
            int x = doorCenter.x;
            foreach (int o in Offsets(width))
                yield return new Vector3Int(x, doorCenter.y + o, 0);
        }
    }

    //방의 Tilemap_Walls에 벽 + 충돌 생성, 바닥 제거
    public void SetWall(Tilemap roomWallsMap, Vector2Int centerWorld, DoorDir dir, int width)
    {
        if (!roomWallsMap)
        {
            Finish(); return;
        }

        string key = (dir == DoorDir.North) ? "Up"
                   : (dir == DoorDir.South) ? "Down"
                   : (dir == DoorDir.East) ? "Right" : "Left";

        TileBase useDirWall = null;

        if (key == "Up" && wallTileUp) useDirWall = wallTileUp;
        if (key == "Down" && wallTileDown) useDirWall = wallTileDown;
        if (key == "Left" && wallTileLeft) useDirWall = wallTileLeft;
        if (key == "Right" && wallTileRight) useDirWall = wallTileRight;

        foreach (var worldCell in GateLineCells(centerWorld, dir, width))
        {
            //전역셀 → 월드 → 방 로컬셀 변환 (로컬에 정확히 찍기)
            Vector3 worldPos = baseMap ? baseMap.CellToWorld(worldCell) : worldCell;
            Vector3Int localCell = roomWallsMap.WorldToCell(worldPos);

            if (baseMap) baseMap.SetTile(worldCell, null);                                      //전역 바닥 제거
            if (roomWallsMap && useDirWall) roomWallsMap.SetTile(localCell, useDirWall);        //로컬(방) Tilemap_Walls에 벽 생성
            if (collisionMap && collisionTile) collisionMap.SetTile(worldCell, collisionTile);  //전역 충돌
        }

        Finish();
    }
}