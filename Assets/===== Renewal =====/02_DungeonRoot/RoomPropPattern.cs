using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Dungeon/RoomPropPattern", fileName = "RoomPropPattern")]
public class RoomPropPattern : ScriptableObject
{
    [Min(1)] public int weight = 1;  //확률 가중치

    [System.Serializable]
    public struct Cell
    {
        public Vector2Int offset;  //앵커 기준
        public TileBase tile;      //찍을 타일
        public bool obstacle;      //충돌 생성 여부 (Props → Collision에 동기화)
    }

    public Cell[] cells = new Cell[] { new Cell { offset = Vector2Int.zero, tile = null, obstacle = false } };

    public bool allowRotate90 = false;
    public bool allowMirrorX = false;
    public bool allowMirrorY = false;
}
