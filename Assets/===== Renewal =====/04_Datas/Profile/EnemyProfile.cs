using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/EnemyProfile", fileName = "EnemyProfile")]
public class EnemyProfile : ScriptableObject
{
    [Header("Melee")]
    public bool enableMelee;      //근접 사용
    public float meleeRange;      //근접 공격 반경 (행동 경계)
    public float meleeExitRange;  //하이브리드 전환 거리 (meleeRange 보다 클 것)

    [Header("Ranged")]
    public bool enableRanged;  //원거리 사용
    public float rangedRange;  //원거리 공격 사거리

    [Header("Behavior Tunings")]
    public float rangeHysteresis;       //경계 덜덜 방지 완충
    public bool backpedalWhenTooClose;  //근접 시 후퇴 (원거리 카이팅)
    public float separationRadius;      //소프트 분리 반경 (서로 너무 붙으면 밀어낼 반경)
    public float separationStrength;    //소프트 분리 강도 (분리 가중치)
}