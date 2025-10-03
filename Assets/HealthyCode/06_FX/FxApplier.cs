using UnityEngine;

[DisallowMultipleComponent]
public class FxApplier : MonoBehaviour
{
    [SerializeField] FxProfile profile;
    [SerializeField] FxRouter router;
    [SerializeField] AnimDriver animDriver;

    private void Awake()
    {
        router.SetRuleSets(profile.ruleSets);  //�������� ��� ����
        animDriver.SetMap(profile.animMap);    //�������� �ִϸ� ����
    }
}