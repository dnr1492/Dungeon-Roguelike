using UnityEngine;

public class FxRouter : MonoBehaviour
{
    [Header("��� (�켱������� �迭)")]
    [SerializeField] FxRuleSet[] ruleSets;

    [Header("�����")]
    [SerializeField] AudioSource sfxSource; 
    [SerializeField] Transform defaultOrigin;

    [Header("�ִϸ��̼�")]
    [SerializeField] AnimDriver animDriver;  //�ִ� ���� ����̹�

    //[Header("ī�޶� ����")]
    //[SerializeField] private CameraFXService cameraFx;

    //������ ��ü�� (����)
    public void SetRuleSets(FxRuleSet[] sets) { ruleSets = sets; }

    public void Handle(FxEventType evt, FxContext ctx)
    {
        //1) �� �˻�
        FxRuleSet.Rule rule = null;
        for (int i = 0; i < ruleSets.Length && rule == null; i++)
            if (ruleSets[i]) rule = ruleSets[i].Resolve(evt, ctx);
        if (rule == null) return;

        //2) ��ġ ���
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

        //5) �ִϸ��̼� (�и��� ����̹� ȣ��)
        if (animDriver) animDriver.Play(evt);

        ////6) ī�޶� FX
        //if (cameraFx)
        //{
        //    if (!string.IsNullOrEmpty(rule.cameraShakeKey))
        //        cameraFx.Shake(rule.cameraShakeKey);
        //    if (rule.hitStopMs > 0)
        //        cameraFx.HitStop(rule.hitStopMs);
        //}
    }
}