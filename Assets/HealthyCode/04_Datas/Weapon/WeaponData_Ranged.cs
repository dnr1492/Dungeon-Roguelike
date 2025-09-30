using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapon/Ranged", fileName = "RangedWeaponData")]
public class WeaponData_Ranged : ScriptableObject
{
    [Header("Fire")]
    public float fireRate;   //���Ÿ� �߻� �ӵ� (�ʴ� �߻�� = ���ݼӵ�)
    public int burst;        //ź ���� �߻�� (1 = �ܹ�)
    public float spreadDeg;  //��ź ���� (����Ʈ > 1�� ���� �ǹ�)

    [Header("Projectile")]
    public float bulletSpeed;   //ź��
    public float bulletLife;    //ź ����
    public float bulletRadius;  //�浹 �ݰ�
    public int bulletDamage;    //ź ������

    [Header("Timing �ɼ� - �ʹ�/������ ���ݿ� ���� ����/���� ���")]
    public float initialFireDelayMin;  //�ʹ� �ּ� ����
    public float initialFireDelayMax;  //�ʹ� �ִ� ����
    public float cooldownJitter;       //��ٿ� �߰� ���� ����

    public float GetInterval() => 1f / Mathf.Max(0.0001f, fireRate);
}