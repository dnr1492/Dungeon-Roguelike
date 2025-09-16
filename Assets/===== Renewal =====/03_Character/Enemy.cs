using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform fireOrigin;  //원거리 사용 시 총구 (없으면 transform)
    
    private readonly float moveSpeed = 2.5f;

    private Transform target;  //Player Transform
    private int attackerId;  //GetInstanceID()
    private bool encounterActive = false;  //방 전투 활성 제어

    #region 근접 공격 (접촉 데미지)
    [Header("Melee Attack")]
    [SerializeField] bool enableMelee = true;
    [SerializeField] Collider2D meleeTrigger;

    private readonly int meleeDamage = 1;
    private readonly float meleeRange = 1f;    //근접 공격 사거리 (트리거 닿는 지점)
    private readonly float tickInterval = 0.4f;  //같은 적 → 같은 대상 타격 간격
    private readonly float knockback = 3f;

    //해당 적(플레이어) 기준 근접 틱 쿨다운
    private readonly Dictionary<int, float> lastHit = new Dictionary<int, float>();
    #endregion

    #region 원거리 공격 (발사체 사격)
    [Header("Ranged Attack")]
    [SerializeField] bool enableRanged = false;
    [SerializeField] Bullet bulletPrefab;

    private readonly float fireRate = 0.2f;      //초당 발사수
    private readonly int burst = 1;              //총 동시 발사수 (1 = 단발)
    private readonly float spreadDeg = 0f;       //총 산탄 각도 (버스트 > 1일 때만 의미)
    private readonly float bulletSpeed = 7f;     //투사체 속도
    private readonly float bulletLife = 2.0f;    //투사체 수명
    private readonly float bulletRadius = 0.1f;  //충돌 반경
    private readonly int bulletDamage = 1;       //총알 데미지
    private readonly float rangedRange = 5f;     //원거리 공격 사거리
    private readonly float meleeExitRange = 3f;  //근접 공격 유지 해제 거리 (meleeRange 보다 클 것)

    private bool meleeEngaged = false;                   //하이브리드 근접 공격 유지 상태
    private readonly float rangeHysteresis = 0.35f;      //경계 덜덜 방지 완충
    private readonly bool backpedalWhenTooClose = true;  //원거리: 너무 가까우면 뒤로 살짝 물러남 (카이팅)
    private float nextFireTime;
    #endregion

    #region 소프트 분리 (겹침 최소화)
    private readonly float separationRadius = 0.6f;    //서로 너무 붙으면 밀어낼 반경
    private readonly float separationStrength = 0.5f;  //분리 가중치
    private readonly Collider2D[] sepBuf = new Collider2D[8];  //GC 없도록 버퍼
    #endregion

    private void Awake()
    {
        attackerId = GetInstanceID();

        //타겟 자동 탐색
        var go = GameObject.FindGameObjectWithTag(ConstClass.Tags.Player);
        if (go) target = go.transform;

        //근접 트리거가 있다면 반드시 isTrigger = true
        if (enableMelee && meleeTrigger)
            meleeTrigger.isTrigger = true;
    }

    private void FixedUpdate()
    {
        if (!encounterActive || !rb) { if (rb) rb.velocity = Vector2.zero; return; }
        if (!target) { rb.velocity = Vector2.zero; return; }

        Vector2 to = target.position - transform.position;
        float dist = to.magnitude;
        Vector2 dir = (dist > 0.0001f) ? (to / dist) : Vector2.zero;

        // ── 이동 정책 ─────────────────────────────────────────────────────────────
        // 1) 하이브리드: '근접 공격 사거리 안' && '근접 공격 유지 해제 거리 안' 이면 '근접 공격' 로직, 바깥이면 '원거리 공격' 로직 (사거리 유지)
        // 2) 근접 전용 (enableMelee만 On): meleeRange 이내로 계속 접근
        // 3) 원거리 전용 (enableRanged만 On): 사거리 유지 (멀면 접근, 너무 가까우면 후퇴 or 정지)
        // ─────────────────────────────────────────────────────────────────────

        if (enableMelee && enableRanged)
        {
            //근접: 충분히 벌어질 때까지 근접 로직 지속
            if (meleeEngaged)
            {
                if (dist > meleeExitRange) meleeEngaged = false;
                rb.velocity = (dist <= meleeRange) 
                    ? Vector2.zero      //근접 공격 사거리 진입 시 정지 (트리거 타격)
                    : moveSpeed * dir;  //그 전에는 계속 접근 (카이팅 금지)
            }
            else
            {
                //근접 전환
                if (dist <= meleeExitRange)
                {
                    if (dist <= meleeRange)
                    {
                        meleeEngaged = true;
                        rb.velocity = Vector2.zero;
                    }
                    else
                    {
                        rb.velocity = moveSpeed * dir;  //붙어서 근접 유도
                    }
                }
                //원거리 공격 유지
                else
                {
                    float near = rangedRange - rangeHysteresis;  //너무 가까워지면 후퇴 시작 경계
                    if (dist > rangedRange) rb.velocity = moveSpeed * dir;  //사거리 바깥 → 계속 접근
                    else if (dist < near) rb.velocity = backpedalWhenTooClose ? -moveSpeed * dir : Vector2.zero;  //너무 가까움 → 후퇴
                    else rb.velocity = Vector2.zero;  //사거리 안쪽 → 정지 (사격)
                }
            }
        }
        //근접 공격 전용
        else if (enableMelee)
        {
            rb.velocity = (dist <= meleeRange) ? Vector2.zero : moveSpeed * dir;
        }
        //원거리 공격 전용
        else if (enableRanged)
        {
            float near = rangedRange - rangeHysteresis;  //너무 가까워지면 후퇴 시작 경계
            if (dist > rangedRange) rb.velocity = moveSpeed * dir;  //사거리 바깥 → 계속 접근
            else if (dist < near) rb.velocity = backpedalWhenTooClose ? -moveSpeed * dir : Vector2.zero;  //너무 가까움 → 후퇴
            else rb.velocity = Vector2.zero;  //사거리 안쪽 → 정지 (사격)
        }
        else
        {
            rb.velocity = moveSpeed * dir;
        }

        //소프트 분리 (적 겹침 최소화): 가까운 Enemy끼리 살짝 밀어낸다.
        //정지 중에도 수행 (겹침 최소화)
        if (rb)
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, separationRadius, sepBuf, ConstClass.Masks.Enemy);
            if (count > 0)
            {
                Vector2 push = Vector2.zero;
                Vector2 self = transform.position;
                for (int i = 0; i < count; i++)
                {
                    var c = sepBuf[i];
                    if (!c || c.attachedRigidbody == rb) continue;
                    Vector2 other = c.attachedRigidbody ? c.attachedRigidbody.position : c.transform.position;
                    Vector2 delta = self - other;
                    float d = delta.magnitude;
                    if (d < 0.0001f) continue;
                    float w = Mathf.InverseLerp(separationRadius, 0f, d);  //가까울수록 가중↑
                    push += delta.normalized * w;
                }
                if (push.sqrMagnitude > 0.0001f)
                {
                    Vector2 sep = push.normalized * separationStrength;
                    rb.velocity += sep;
                }
            }
        }

        //전방 감속 (벽 코앞 박치기 방지)
        if (rb.velocity.sqrMagnitude > 0.0001f)
        {
            var hit = Physics2D.Raycast(transform.position, dir, 0.5f, ConstClass.Masks.Wall);
            if (hit.collider) rb.velocity *= 0.25f;
        }
    }

    private void Update()
    {
        if (!encounterActive || !target) return;
        if (!enableRanged || !bulletPrefab) return;

        //하이브리드에서 근접 공격 유지 중에는 원거리 공격 금지
        if (enableMelee && enableRanged && meleeEngaged) return;

        float dist = Vector2.Distance(transform.position, target.position);

        //원거리: '공격 사거리 안'에서만 발사 (쿨다운 기준)
        if (dist <= rangedRange && Time.time >= nextFireTime)
        {
            FireAt(target.position);
            nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, fireRate);
        }
    }

    //근접 공격인 경우에만 호출
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!encounterActive) return;
        if (!enableMelee) return;
        if (!meleeTrigger) return;

        if (other == null) return;

        //상대가 Player인지 (레이어/태그 둘 다 체크)
        bool isPlayer = ((1 << other.gameObject.layer) & ConstClass.Masks.Player) != 0 || other.CompareTag(ConstClass.Tags.Player);
        if (!isPlayer) return;

        var h = other.GetComponentInParent<Health>();
        if (!h) return;

        //틱 간격 체크
        int targetId = h.GetInstanceID();
        float now = Time.time;
        if (lastHit.TryGetValue(targetId, out var last) && now - last < tickInterval) return;
        lastHit[targetId] = now;

        //근접 데미지: 공격자 ID를 넘겨 '공격자별 i-frame' 적용
        //플레이어 Health에서 usePerSourceIFrame = true일 때만 의미가 있음
        h.TakeHit(meleeDamage, attackerId);

        //넉백
        if (knockback > 0f && h.TryGetComponent<Rigidbody2D>(out var prb))
        {
            Vector2 dir = (other.bounds.center - meleeTrigger.bounds.center);
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
            prb.AddForce(dir * knockback, ForceMode2D.Impulse);
        }
    }

    //해당 적의 전투 활성화/비활성화
    public void SetEncounterActive(bool active)
    {
        encounterActive = active;

        if (!encounterActive && rb)
            rb.velocity = Vector2.zero;  //비활성 시 이동 정지

        lastHit.Clear();  //근접 틱 기록 리셋
        meleeEngaged = false;  //근접 공격 유지 상태 리셋
    }

    #region 무기 발사
    private void FireAt(Vector3 worldPos)
    {
        if (!bulletPrefab) return;

        Vector3 origin = fireOrigin ? fireOrigin.position : transform.position;
        Vector2 to = worldPos - origin;
        float baseZ = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;

        int n = Mathf.Max(1, burst);
        float step = (n > 1) ? (spreadDeg / (n - 1)) : 0f;
        float start = -spreadDeg * 0.5f;

        var ignore = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < n; i++)
        {
            float z = baseZ + (n == 1 ? 0f : start + step * i);

            var b = GameManager.Instance.GetBullet(bulletPrefab);
            b.Spawn(
                origin,
                Quaternion.Euler(0, 0, z),
                bulletSpeed, bulletLife, bulletRadius, bulletDamage,
                ConstClass.Masks.Player,  //적 총알은 플레이어만 맞춤
                ignore,
                GameManager.Instance.ReturnBullet
            );
        }
    }
    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        bool drawMelee = enableMelee;
        bool drawRanged = enableRanged;

        Vector3 p = transform.position;

        if (drawMelee)
        {
            //빨강: meleeRange (근접 타격이 실제로 들어가는 반경)
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.55f);
            Gizmos.DrawWireSphere(p, meleeRange);

            //주황: meleeExitRange (근접 상태 해제 / 하이브리드 전환 외곽 경계)
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.30f);
            Gizmos.DrawWireSphere(p, meleeExitRange);

            //라벨 (편의용)
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.8f);
            UnityEditor.Handles.Label(p + Vector3.right * (meleeRange + 0.05f), "meleeRange");
            UnityEditor.Handles.Label(p + Vector3.right * (meleeExitRange + 0.05f), "meleeExitRange");
        }

        if (drawRanged && rangedRange > 0f)
        {
            //파랑 (진): rangedRange (원거리 공격 사거리)
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.85f);
            Gizmos.DrawWireSphere(p, rangedRange);

            if (rangeHysteresis > 0f)
            {
                //파랑 (옅-내부): near = rangedRange - rangeHysteresis (너무 가까우면 후퇴 시작 경계)
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
                Gizmos.DrawWireSphere(p, rangedRange - rangeHysteresis);

                //파랑 (옅-외부): far = rangedRange + rangeHysteresis (참고용 표식)
                Gizmos.DrawWireSphere(p, rangedRange + rangeHysteresis);
            }

            //라벨(편의용)
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.8f);
            UnityEditor.Handles.Label(p + Vector3.up * 0.05f + Vector3.right * (rangedRange + 0.05f), "rangedRange");
            if (rangeHysteresis > 0f)
            {
                UnityEditor.Handles.Label(p + Vector3.right * (rangedRange - rangeHysteresis + 0.05f), "near");
                UnityEditor.Handles.Label(p + Vector3.right * (rangedRange + rangeHysteresis + 0.05f), "far (ref)");
            }
        }
    }
#endif
}