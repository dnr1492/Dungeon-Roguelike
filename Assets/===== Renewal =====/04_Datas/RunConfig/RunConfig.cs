using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/RunConfig", fileName = "RunConfig")]
public class RunConfig : ScriptableObject
{
    [Header("Theme / Dungeon Meta")]
    public ThemeId themeId = ThemeId.Dungeon;  //테마 선택

    [System.Serializable]
    public struct QuotaRange { public int min, max; }

    [System.Serializable]
    public struct DungeonMeta
    {
        public QuotaRange quotaCombat;  //전투방
        public QuotaRange quotaElite;   //정예방
        public int quotaBoss;           //보스방
        public QuotaRange quotaShop;    //상점방
        public QuotaRange quotaEvent;   //이벤트방
    }
    public DungeonMeta meta;

    [System.Serializable]
    public class EncounterSet
    {
        [Header("Pool (가중 스폰은 프리팹을 중복 등록해서 가중치 표현)")]
        public List<GameObject> enemyPool = new();

        [Header("웨이브 예산/수량")]
        public Vector2Int countRange = new Vector2Int(8, 16);  //min/max

        [Header("배치 제약")]
        public int minDistFromDoor = 2;
        public int minDistFromWall = 1;
        public int minEnemySpacing = 2;
    }

    [Header("Encounters")]
    public EncounterSet combat = new EncounterSet();
    public EncounterSet elite = new EncounterSet();
}
