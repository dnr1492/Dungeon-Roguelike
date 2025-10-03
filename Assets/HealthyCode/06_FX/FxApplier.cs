using UnityEngine;

[DisallowMultipleComponent]
public class FxApplier : MonoBehaviour
{
    [SerializeField] FxProfile profile;
    [SerializeField] FxRouter router;
    [SerializeField] AnimDriver animDriver;

    private void Awake()
    {
        router.SetRuleSets(profile.ruleSets);  //프로필의 룰셋 주입
        animDriver.SetMap(profile.animMap);    //프로필의 애니맵 주입
    }
}