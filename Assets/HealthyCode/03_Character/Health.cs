using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField] CharacterStats stats;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] UIDamageFloatingText damageFloatingTextPrefab;
    [SerializeField] Transform damageFloatingTextPivot;

    public int CurHP { get; private set; }
    public int MaxHP => stats.maxHP;

    public event Action OnDeath;
    public event Action<int, int> OnHealthChanged;  //curHp, maxHp

    #region 히트 플래시
    private Color originColor;
    private Color hitColor = new Color(1f, 0.4f, 0.4f, 1f);
    private int flashVer;

    private readonly float hitFlashTime = 0.06f;
    #endregion

    private readonly Dictionary<int, float> lastHitBy = new();  //최근 피격 기록 (공격자 ID → 마지막 시간)

    private void Awake()
    {
        CurHP = stats.maxHP;

        originColor = spriteRenderer.color;

        OnHealthChanged?.Invoke(CurHP, MaxHP);
    }

    /// <summary>
    /// 단순 데미지 적용 (공격자 ID 없음). 총알/트랩/환경에 사용.
    /// '공격자별 i-frame' 로직은 타지 않는다.
    /// </summary>
    public void TakeDamage(int dmg) => TakeHit(dmg, 0);

    /// <summary>
    /// 공격자 ID를 동반한 데미지 적용. 근접/틱딜 등 '같은 공격자'의 연속 히트를
    /// perSourceIFrame 내에서만 무시한다. 다른 공격자/다른 총알은 그대로 들어간다.
    /// attackerId에는 보통 attacker.GetInstanceID()를 넣는다. (0을 넣으면 i-frame 미적용)
    /// </summary>
    public void TakeHit(int dmg, int attackerId)
    {
        if (CurHP <= 0) return;

        if (stats.usePerSourceIFrame && attackerId != 0)
        {
            float now = Time.time;
            if (lastHitBy.TryGetValue(attackerId, out var last) && now - last < stats.perSourceIFrame)
                return;
            lastHitBy[attackerId] = now;
        }

        CurHP -= dmg;

        //히트 플래시
        if (spriteRenderer) HitFlashAsync().Forget();

        OnHealthChanged?.Invoke(CurHP, MaxHP);

        var dt = GameManager.Instance.GetDamageFloatingText(damageFloatingTextPrefab);
        if (dt) dt.Show(dmg, damageFloatingTextPivot.position, Color.red, GameManager.Instance.ReturnDamageFloatingText);

        if (CurHP <= 0)
        {
            OnDeath.Invoke();
            if (stats.destroyOnDeath) Destroy(gameObject);
        }
    }

    /// <summary>
    /// 피격 시 색상 표시
    /// </summary>
    /// <returns></returns>
    private async UniTaskVoid HitFlashAsync()
    {
        flashVer++;
        int myVer = flashVer; 

        spriteRenderer.color = hitColor;

        //파괴 시 자동으로 취소되는 토큰 사용
        var token = this.GetCancellationTokenOnDestroy();
        await UniTask.Delay(TimeSpan.FromSeconds(hitFlashTime), cancellationToken: token);

        //더 최근 히트가 있으면 색 복구를 내버려 둠
        if (myVer == flashVer && spriteRenderer) spriteRenderer.color = originColor;
    }
}