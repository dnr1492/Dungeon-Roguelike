using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/CharacterStats", fileName = "CharacterStats")]
public class CharacterStats : ScriptableObject
{
    [Header("이동/락온")]
    public float moveSpeed;   //이동 속도
    public float stickyTime;  //락온 유지시간 (0이면 즉시 재선정)

    [Header("체력/피격 정책 (Health에 적용)")]
    public int maxHP;                //최대 체력
    public bool destroyOnDeath;      //죽을 시 즉시 파괴 유무
    public bool usePerSourceIFrame;  //동일 공격자에게 피격 시 무적 유무 (Player는 true, Enemy는 false 추천)
    public float perSourceIFrame;    //동일 공격자에게 피격 시 무적 시간
}
