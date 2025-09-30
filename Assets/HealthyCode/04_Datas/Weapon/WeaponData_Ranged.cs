using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapon/Ranged", fileName = "RangedWeaponData")]
public class WeaponData_Ranged : ScriptableObject
{
    [Header("Fire")]
    public float fireRate;   //원거리 발사 속도 (초당 발사수 = 공격속도)
    public int burst;        //탄 동시 발사수 (1 = 단발)
    public float spreadDeg;  //산탄 각도 (버스트 > 1일 때만 의미)

    [Header("Projectile")]
    public float bulletSpeed;   //탄속
    public float bulletLife;    //탄 수명
    public float bulletRadius;  //충돌 반경
    public int bulletDamage;    //탄 데미지

    [Header("Timing 옵션 - 초발/재장전 간격에 섞는 랜덤/지연 요소")]
    public float initialFireDelayMin;  //초발 최소 지연
    public float initialFireDelayMax;  //초발 최대 지연
    public float cooldownJitter;       //쿨다운 추가 랜덤 지터

    public float GetInterval() => 1f / Mathf.Max(0.0001f, fireRate);
}