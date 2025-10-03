using UnityEngine;

[CreateAssetMenu(menuName = "Game/FX/FxProfile", fileName = "FxProfile")]
public class FxProfile : ScriptableObject
{
    public FxRuleSet[] ruleSets;  //�� ��ü�� ����� SFX/VFX ��Ģ (�켱���� ��)
    public AnimMap animMap;       //�� ��ü�� ����� �ִϸ��̼� ����
}