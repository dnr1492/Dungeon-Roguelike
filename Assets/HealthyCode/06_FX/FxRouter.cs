using UnityEngine;

public class FxRouter : MonoBehaviour
{
    [Header("룰셋 (우선순위대로 배열)")]
    [SerializeField] FxRuleSet[] ruleSets;

    [Header("재생기")]
    [SerializeField] AudioSource sfxSource; 
    [SerializeField] Transform defaultOrigin;

    [Header("애니메이션")]
    [SerializeField] AnimDriver animDriver;  //애니 전담 드라이버

    //[Header("카메라 연출")]
    //[SerializeField] private CameraFXService cameraFx;

    //프로필 교체용 (세터)
    public void SetRuleSets(FxRuleSet[] sets) { ruleSets = sets; }

    public void Handle(FxEventType evt, FxContext ctx)
    {
        //1) 룰 검색
        FxRuleSet.Rule rule = null;
        for (int i = 0; i < ruleSets.Length && rule == null; i++)
            if (ruleSets[i]) rule = ruleSets[i].Resolve(evt, ctx);
        if (rule == null) return;

        //2) 위치 계산
        Vector3 pos = ctx.hitPos != default ? ctx.hitPos
                   : (ctx.origin ? ctx.origin.position
                   : (defaultOrigin ? defaultOrigin.position : Vector3.zero));

        //3) SFX
        if (rule.sfx && sfxSource)
            sfxSource.PlayOneShot(rule.sfx);

        //4) VFX
        if (rule.vfxPrefab)
        {
            var go = Instantiate(rule.vfxPrefab, pos, Quaternion.identity);
            if (rule.vfxLife > 0f) Destroy(go, rule.vfxLife);
        }

        //5) 애니메이션 (분리된 드라이버 호출)
        if (animDriver) animDriver.Play(evt);

        ////6) 카메라 FX
        //if (cameraFx)
        //{
        //    if (!string.IsNullOrEmpty(rule.cameraShakeKey))
        //        cameraFx.Shake(rule.cameraShakeKey);
        //    if (rule.hitStopMs > 0)
        //        cameraFx.HitStop(rule.hitStopMs);
        //}
    }
}