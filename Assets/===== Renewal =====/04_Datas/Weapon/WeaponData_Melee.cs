using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapon/Melee", fileName = "MeleeWeaponData")]
public class WeaponData_Melee : ScriptableObject
{
    public int meleeDamage;          //���� ������
    public float meleeTickInterval;  //���� ƽ ���� (���� �� �� ���� ��� Ÿ�� ����)
    public float meleeKnockback;     //���� �˹�
}