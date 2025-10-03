using UnityEngine;

[CreateAssetMenu(menuName = "Game/FX/FxProfile", fileName = "FxProfile")]
public class FxProfile : ScriptableObject
{
    public FxRuleSet[] ruleSets;  //이 개체가 사용할 SFX/VFX 규칙 (우선순위 순)
    public AnimMap animMap;       //이 개체가 사용할 애니메이션 매핑
}