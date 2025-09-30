using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/CharacterStats", fileName = "CharacterStats")]
public class CharacterStats : ScriptableObject
{
    [Header("�̵�/����")]
    public float moveSpeed;   //�̵� �ӵ�
    public float stickyTime;  //���� �����ð� (0�̸� ��� �缱��)

    [Header("ü��/�ǰ� ��å (Health�� ����)")]
    public int maxHP;                //�ִ� ü��
    public bool destroyOnDeath;      //���� �� ��� �ı� ����
    public bool usePerSourceIFrame;  //���� �����ڿ��� �ǰ� �� ���� ���� (Player�� true, Enemy�� false ��õ)
    public float perSourceIFrame;    //���� �����ڿ��� �ǰ� �� ���� �ð�
}
