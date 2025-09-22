using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    [SerializeField] EnemyProfile profile;
    [SerializeField] CharacterStats stats;
    [SerializeField] WeaponData_Ranged rangedData;
    [SerializeField] WeaponData_Melee meleeData;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform fireOrigin;  //원거리 사용 시 총구 (없으면 transform)
    
    private Transform target;  //Player Transform
    private int attackerId;  //GetInstanceID()
    private bool encounterActive = false;  //방 전투 활성 제어

    #region 근접 공격 (feat.접촉 데미지)
    [Header("Melee Attack")]
    [SerializeField] Collider2D meleeTrigger;

    private readonly Dictionary<int, float> lastHit = new Dictionary<int, float>();  //해당 적(플레이어) 기준 근접 틱 쿨다운
    #endregion

    #region 원거리 공격 (발사체 사격)
    [Header("Ranged Attack")]
    [SerializeField] Bullet bulletPrefab;

    private bool meleeEngaged = false;  //하이브리드인 경우 근접 공격 유지 상태
    private float nextFireTime;
    #endregion

    #region 소프트 분리 (겹침 최소화)
    private readonly Collider2D[] sepBuf = new Collider2D[8];  //GC 없도록 버퍼
    #endregion

    private void Awake()
    {
        attackerId = GetInstanceID();

        //타겟 자동 탐색
        var go = GameObject.FindGameObjectWithTag(ConstClass.Tags.Player);
        if (go) target = go.transform;

        //근접 트리거가 있다면 반드시 isTrigger = true
        if (profile.enableMelee && meleeTrigger)
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

        if (profile.enableMelee && profile.enableRanged)
        {
            //근접: 충분히 벌어질 때까지 근접 로직 지속
            if (meleeEngaged)
            {
                if (dist > profile.meleeExitRange) meleeEngaged = false;
                rb.velocity = (dist <= profile.meleeRange) 
                    ? Vector2.zero      //근접 공격 사거리 진입 시 정지 (트리거 타격)
                    : stats.moveSpeed * dir;  //그 전에는 계속 접근 (카이팅 금지)
            }
            else
            {
                //근접 전환
                if (dist <= profile.meleeExitRange)
                {
                    if (dist <= profile.meleeRange)
                    {
                        meleeEngaged = true;
                        rb.velocity = Vector2.zero;
                    }
                    else
                    {
                        rb.velocity = stats.moveSpeed * dir;  //붙어서 근접 유도
                    }
                }
                //원거리 공격 유지
                else
                {
                    float near = profile.rangedRange - profile.rangeHysteresis;  //너무 가까워지면 후퇴 시작 경계
                    if (dist > profile.rangedRange) rb.velocity = stats.moveSpeed * dir;  //사거리 바깥 → 계속 접근
                    else if (dist < near) rb.velocity = profile.backpedalWhenTooClose ? -stats.moveSpeed * dir : Vector2.zero;  //너무 가까움 → 후퇴
                    else rb.velocity = Vector2.zero;  //사거리 안쪽 → 정지 (사격)
                }
            }
        }
        //근접 공격 전용
        else if (profile.enableMelee)
        {
            rb.velocity = (dist <= profile.meleeRange) ? Vector2.zero : stats.moveSpeed * dir;
        }
        //원거리 공격 전용
        else if (profile.enableRanged)
        {
            float near = profile.rangedRange - profile.rangeHysteresis;  //너무 가까워지면 후퇴 시작 경계
            if (dist > profile.rangedRange) rb.velocity = stats.moveSpeed * dir;  //사거리 바깥 → 계속 접근
            else if (dist < near) rb.velocity = profile.backpedalWhenTooClose ? -stats.moveSpeed * dir : Vector2.zero;  //너무 가까움 → 후퇴
            else rb.velocity = Vector2.zero;  //사거리 안쪽 → 정지 (사격)
        }
        else
        {
            rb.velocity = stats.moveSpeed * dir;
        }

        //소프트 분리 (적 겹침 최소화): 가까운 Enemy끼리 살짝 밀어낸다.
        //정지 중에도 수행 (겹침 최소화)
        if (rb)
        {
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, profile.separationRadius, sepBuf, ConstClass.Masks.Enemy);
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
                    float w = Mathf.InverseLerp(profile.separationRadius, 0f, d);  //가까울수록 가중↑
                    push += delta.normalized * w;
                }
                if (push.sqrMagnitude > 0.0001f)
                {
                    Vector2 sep = push.normalized * profile.separationStrength;
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
        if (!profile.enableRanged || !bulletPrefab) return;

        //하이브리드에서 근접 공격 유지 중에는 원거리 공격 금지
        if (profile.enableMelee && profile.enableRanged && meleeEngaged) return;

        float dist = Vector2.Distance(transform.position, target.position);

        //원거리: '공격 사거리 안'에서만 발사 (쿨다운 기준)
        if (dist <= profile.rangedRange && Time.time >= nextFireTime)
        {
            FireAt(target.position);

            float interval = rangedData.GetInterval();
            float jitter = rangedData.cooldownJitter * Random.value;
            nextFireTime = Time.time + interval + jitter;
        }
    }

    //근접 공격인 경우에만 호출
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!encounterActive) return;
        if (!profile.enableMelee) return;
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
        if (lastHit.TryGetValue(targetId, out var last) && now - last < meleeData.meleeTickInterval) return;
        lastHit[targetId] = now;

        //근접 데미지: 공격자 ID를 넘겨 '공격자별 i-frame' 적용
        //플레이어 Health에서 usePerSourceIFrame = true일 때만 의미가 있음
        h.TakeHit(meleeData.meleeDamage, attackerId);

        //넉백
        if (meleeData.meleeKnockback > 0f && h.TryGetComponent<Rigidbody2D>(out var prb))
        {
            Vector2 dir = (other.bounds.center - meleeTrigger.bounds.center);
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
            prb.AddForce(dir * meleeData.meleeKnockback, ForceMode2D.Impulse);
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

        if (encounterActive && rangedData != null) {
            nextFireTime = Time.time + Random.Range(rangedData.initialFireDelayMin, rangedData.initialFireDelayMax);
        }
    }

    #region 무기 발사
    private void FireAt(Vector3 worldPos)
    {
        if (!bulletPrefab) return;

        Vector3 origin = fireOrigin ? fireOrigin.position : transform.position;
        Vector2 to = worldPos - origin;
        float baseZ = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;

        int n = Mathf.Max(1, rangedData.burst);
        float step = (n > 1) ? (rangedData.spreadDeg / (n - 1)) : 0f;
        float start = -rangedData.spreadDeg * 0.5f;

        var ignore = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < n; i++)
        {
            float z = baseZ + (n == 1 ? 0f : start + step * i);

            var b = GameManager.Instance.GetBullet(bulletPrefab);
            b.Spawn(
                origin,
                Quaternion.Euler(0, 0, z),
                rangedData.bulletSpeed, rangedData.bulletLife, rangedData.bulletRadius, rangedData.bulletDamage,
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
        bool drawMelee = profile.enableMelee;
        bool drawRanged = profile.enableRanged;

        Vector3 p = transform.position;

        if (drawMelee)
        {
            //빨강: meleeRange (근접 타격이 실제로 들어가는 반경)
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.55f);
            Gizmos.DrawWireSphere(p, profile.meleeRange);

            //주황: meleeExitRange (근접 상태 해제 / 하이브리드 전환 외곽 경계)
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.30f);
            Gizmos.DrawWireSphere(p, profile.meleeExitRange);

            //라벨 (편의용)
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.8f);
            UnityEditor.Handles.Label(p + Vector3.right * (profile.meleeRange + 0.05f), "meleeRange");
            UnityEditor.Handles.Label(p + Vector3.right * (profile.meleeExitRange + 0.05f), "meleeExitRange");
        }

        if (drawRanged && profile.rangedRange > 0f)
        {
            //파랑 (진): rangedRange (원거리 공격 사거리)
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.85f);
            Gizmos.DrawWireSphere(p, profile.rangedRange);

            if (profile.rangeHysteresis > 0f)
            {
                //파랑 (옅-내부): near = rangedRange - rangeHysteresis (너무 가까우면 후퇴 시작 경계)
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
                Gizmos.DrawWireSphere(p, profile.rangedRange - profile.rangeHysteresis);

                //파랑 (옅-외부): far = rangedRange + rangeHysteresis (참고용 표식)
                Gizmos.DrawWireSphere(p, profile.rangedRange + profile.rangeHysteresis);
            }

            //라벨(편의용)
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.8f);
            UnityEditor.Handles.Label(p + Vector3.up * 0.05f + Vector3.right * (profile.rangedRange + 0.05f), "rangedRange");
            if (profile.rangeHysteresis > 0f)
            {
                UnityEditor.Handles.Label(p + Vector3.right * (profile.rangedRange - profile.rangeHysteresis + 0.05f), "near");
                UnityEditor.Handles.Label(p + Vector3.right * (profile.rangedRange + profile.rangeHysteresis + 0.05f), "far (ref)");
            }
        }
    }
#endif
}