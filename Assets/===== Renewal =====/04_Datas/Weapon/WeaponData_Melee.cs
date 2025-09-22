using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapon/Melee", fileName = "MeleeWeaponData")]
public class WeaponData_Melee : ScriptableObject
{
    public int meleeDamage;          //근접 데미지
    public float meleeTickInterval;  //근접 틱 간격 (같은 적 → 같은 대상 타격 간격)
    public float meleeKnockback;     //근접 넉백
}