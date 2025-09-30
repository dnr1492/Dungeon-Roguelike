using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/RunConfig", fileName = "RunConfig")]
public class RunConfig : ScriptableObject
{
    [Header("Theme / Dungeon Meta")]
    public ThemeId themeId = ThemeId.Dungeon;  //�׸� ����

    [System.Serializable]
    public struct QuotaRange { public int min, max; }

    [System.Serializable]
    public struct DungeonMeta
    {
        public QuotaRange quotaCombat;  //������
        public QuotaRange quotaElite;   //������
        public int quotaBoss;           //������
        public QuotaRange quotaShop;    //������
        public QuotaRange quotaEvent;   //�̺�Ʈ��
    }
    public DungeonMeta meta;

    [System.Serializable]
    public class EncounterSet
    {
        [Header("Pool (���� ������ �������� �ߺ� ����ؼ� ����ġ ǥ��)")]
        public List<GameObject> enemyPool = new();

        [Header("���̺� ����/����")]
        public Vector2Int countRange = new Vector2Int(8, 16);  //min/max

        [Header("��ġ ����")]
        public int minDistFromDoor = 2;
        public int minDistFromWall = 1;
        public int minEnemySpacing = 2;
    }

    [Header("Encounters")]
    public EncounterSet combat = new EncounterSet();
    public EncounterSet elite = new EncounterSet();
}
