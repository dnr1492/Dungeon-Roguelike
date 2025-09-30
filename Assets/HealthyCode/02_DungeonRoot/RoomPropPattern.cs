using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Dungeon/RoomPropPattern", fileName = "RoomPropPattern")]
public class RoomPropPattern : ScriptableObject
{
    [Min(1)] public int weight = 1;  //Ȯ�� ����ġ

    [System.Serializable]
    public struct Cell
    {
        public Vector2Int offset;  //��Ŀ ����
        public TileBase tile;      //���� Ÿ��
        public bool obstacle;      //�浹 ���� ���� (Props �� Collision�� ����ȭ)
    }

    public Cell[] cells = new Cell[] { new Cell { offset = Vector2Int.zero, tile = null, obstacle = false } };

    public bool allowRotate90 = false;
    public bool allowMirrorX = false;
    public bool allowMirrorY = false;
}
