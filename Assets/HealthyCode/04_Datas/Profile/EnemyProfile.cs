using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/EnemyProfile", fileName = "EnemyProfile")]
public class EnemyProfile : ScriptableObject
{
    [Header("Melee")]
    public bool enableMelee;      //���� ���
    public float meleeRange;      //���� ���� �ݰ� (�ൿ ���)
    public float meleeExitRange;  //���̺긮�� ��ȯ �Ÿ� (meleeRange ���� Ŭ ��)

    [Header("Ranged")]
    public bool enableRanged;  //���Ÿ� ���
    public float rangedRange;  //���Ÿ� ���� ��Ÿ�

    [Header("Behavior Tunings")]
    public float rangeHysteresis;       //��� ���� ���� ����
    public bool backpedalWhenTooClose;  //���� �� ���� (���Ÿ� ī����)
    public float separationRadius;      //����Ʈ �и� �ݰ� (���� �ʹ� ������ �о �ݰ�)
    public float separationStrength;    //����Ʈ �и� ���� (�и� ����ġ)
}