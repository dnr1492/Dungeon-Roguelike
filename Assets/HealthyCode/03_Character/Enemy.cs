using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour
{
    [SerializeField] EnemyProfile profile;
    [SerializeField] CharacterStats stats;
    [SerializeField] WeaponData_Ranged rangedData;
    [SerializeField] WeaponData_Melee meleeData;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform fireOrigin;  //���Ÿ� ��� �� �ѱ� (������ transform)
    
    private Transform target;  //Player Transform
    private int attackerId;  //GetInstanceID()
    private bool encounterActive = false;  //�� ���� Ȱ�� ����

    #region ���� ���� (feat.���� ������)
    [Header("Melee Attack")]
    [SerializeField] Collider2D meleeTrigger;

    private readonly Dictionary<int, float> lastHit = new Dictionary<int, float>();  //�ش� ��(�÷��̾�) ���� ���� ƽ ��ٿ�
    #endregion

    #region ���Ÿ� ���� (�߻�ü ���)
    [Header("Ranged Attack")]
    [SerializeField] Bullet bulletPrefab;

    private bool meleeEngaged = false;  //���̺긮���� ��� ���� ���� ���� ����
    private float nextFireTime;
    #endregion

    #region ����Ʈ �и� (��ħ �ּ�ȭ)
    private readonly Collider2D[] sepBuf = new Collider2D[8];  //GC ������ ����
    #endregion

    private void Awake()
    {
        attackerId = GetInstanceID();

        //Ÿ�� �ڵ� Ž��
        var go = GameObject.FindGameObjectWithTag(ConstClass.Tags.Player);
        if (go) target = go.transform;

        //���� Ʈ���Ű� �ִٸ� �ݵ�� isTrigger = true
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

        // ���� �̵� ��å ��������������������������������������������������������������������������������������������������������������������������
        // 1) ���̺긮��: '���� ���� ��Ÿ� ��' && '���� ���� ���� ���� �Ÿ� ��' �̸� '���� ����' ����, �ٱ��̸� '���Ÿ� ����' ���� (��Ÿ� ����)
        // 2) ���� ���� (enableMelee�� On): meleeRange �̳��� ��� ����
        // 3) ���Ÿ� ���� (enableRanged�� On): ��Ÿ� ���� (�ָ� ����, �ʹ� ������ ���� or ����)
        // ������������������������������������������������������������������������������������������������������������������������������������������

        if (profile.enableMelee && profile.enableRanged)
        {
            //����: ����� ������ ������ ���� ���� ����
            if (meleeEngaged)
            {
                if (dist > profile.meleeExitRange) meleeEngaged = false;
                rb.velocity = (dist <= profile.meleeRange) 
                    ? Vector2.zero      //���� ���� ��Ÿ� ���� �� ���� (Ʈ���� Ÿ��)
                    : stats.moveSpeed * dir;  //�� ������ ��� ���� (ī���� ����)
            }
            else
            {
                //���� ��ȯ
                if (dist <= profile.meleeExitRange)
                {
                    if (dist <= profile.meleeRange)
                    {
                        meleeEngaged = true;
                        rb.velocity = Vector2.zero;
                    }
                    else
                    {
                        rb.velocity = stats.moveSpeed * dir;  //�پ ���� ����
                    }
                }
                //���Ÿ� ���� ����
                else
                {
                    float near = profile.rangedRange - profile.rangeHysteresis;  //�ʹ� ��������� ���� ���� ���
                    if (dist > profile.rangedRange) rb.velocity = stats.moveSpeed * dir;  //��Ÿ� �ٱ� �� ��� ����
                    else if (dist < near) rb.velocity = profile.backpedalWhenTooClose ? -stats.moveSpeed * dir : Vector2.zero;  //�ʹ� ����� �� ����
                    else rb.velocity = Vector2.zero;  //��Ÿ� ���� �� ���� (���)
                }
            }
        }
        //���� ���� ����
        else if (profile.enableMelee)
        {
            rb.velocity = (dist <= profile.meleeRange) ? Vector2.zero : stats.moveSpeed * dir;
        }
        //���Ÿ� ���� ����
        else if (profile.enableRanged)
        {
            float near = profile.rangedRange - profile.rangeHysteresis;  //�ʹ� ��������� ���� ���� ���
            if (dist > profile.rangedRange) rb.velocity = stats.moveSpeed * dir;  //��Ÿ� �ٱ� �� ��� ����
            else if (dist < near) rb.velocity = profile.backpedalWhenTooClose ? -stats.moveSpeed * dir : Vector2.zero;  //�ʹ� ����� �� ����
            else rb.velocity = Vector2.zero;  //��Ÿ� ���� �� ���� (���)
        }
        else
        {
            rb.velocity = stats.moveSpeed * dir;
        }

        //����Ʈ �и� (�� ��ħ �ּ�ȭ): ����� Enemy���� ��¦ �о��.
        //���� �߿��� ���� (��ħ �ּ�ȭ)
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
                    float w = Mathf.InverseLerp(profile.separationRadius, 0f, d);  //�������� ���ߡ�
                    push += delta.normalized * w;
                }
                if (push.sqrMagnitude > 0.0001f)
                {
                    Vector2 sep = push.normalized * profile.separationStrength;
                    rb.velocity += sep;
                }
            }
        }

        //���� ���� (�� �ھ� ��ġ�� ����)
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

        //���̺긮�忡�� ���� ���� ���� �߿��� ���Ÿ� ���� ����
        if (profile.enableMelee && profile.enableRanged && meleeEngaged) return;

        float dist = Vector2.Distance(transform.position, target.position);

        //���Ÿ�: '���� ��Ÿ� ��'������ �߻� (��ٿ� ����)
        if (dist <= profile.rangedRange && Time.time >= nextFireTime)
        {
            FireAt(target.position);

            float interval = rangedData.GetInterval();
            float jitter = rangedData.cooldownJitter * Random.value;
            nextFireTime = Time.time + interval + jitter;
        }
    }

    //���� ������ ��쿡�� ȣ��
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!encounterActive) return;
        if (!profile.enableMelee) return;
        if (!meleeTrigger) return;

        if (other == null) return;

        //��밡 Player���� (���̾�/�±� �� �� üũ)
        bool isPlayer = ((1 << other.gameObject.layer) & ConstClass.Masks.Player) != 0 || other.CompareTag(ConstClass.Tags.Player);
        if (!isPlayer) return;

        var h = other.GetComponentInParent<Health>();
        if (!h) return;

        //ƽ ���� üũ
        int targetId = h.GetInstanceID();
        float now = Time.time;
        if (lastHit.TryGetValue(targetId, out var last) && now - last < meleeData.meleeTickInterval) return;
        lastHit[targetId] = now;

        //���� ������: ������ ID�� �Ѱ� '�����ں� i-frame' ����
        //�÷��̾� Health���� usePerSourceIFrame = true�� ���� �ǹ̰� ����
        h.TakeHit(meleeData.meleeDamage, attackerId);

        //�˹�
        if (meleeData.meleeKnockback > 0f && h.TryGetComponent<Rigidbody2D>(out var prb))
        {
            Vector2 dir = (other.bounds.center - meleeTrigger.bounds.center);
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
            prb.AddForce(dir * meleeData.meleeKnockback, ForceMode2D.Impulse);
        }
    }

    //�ش� ���� ���� Ȱ��ȭ/��Ȱ��ȭ
    public void SetEncounterActive(bool active)
    {
        encounterActive = active;

        if (!encounterActive && rb)
            rb.velocity = Vector2.zero;  //��Ȱ�� �� �̵� ����

        lastHit.Clear();  //���� ƽ ��� ����
        meleeEngaged = false;  //���� ���� ���� ���� ����

        if (encounterActive && rangedData != null) {
            nextFireTime = Time.time + Random.Range(rangedData.initialFireDelayMin, rangedData.initialFireDelayMax);
        }
    }

    #region ���� �߻�
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
                ConstClass.Masks.Player,  //�� �Ѿ��� �÷��̾ ����
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
            //����: meleeRange (���� Ÿ���� ������ ���� �ݰ�)
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.55f);
            Gizmos.DrawWireSphere(p, profile.meleeRange);

            //��Ȳ: meleeExitRange (���� ���� ���� / ���̺긮�� ��ȯ �ܰ� ���)
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.30f);
            Gizmos.DrawWireSphere(p, profile.meleeExitRange);

            //�� (���ǿ�)
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.8f);
            UnityEditor.Handles.Label(p + Vector3.right * (profile.meleeRange + 0.05f), "meleeRange");
            UnityEditor.Handles.Label(p + Vector3.right * (profile.meleeExitRange + 0.05f), "meleeExitRange");
        }

        if (drawRanged && profile.rangedRange > 0f)
        {
            //�Ķ� (��): rangedRange (���Ÿ� ���� ��Ÿ�)
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.85f);
            Gizmos.DrawWireSphere(p, profile.rangedRange);

            if (profile.rangeHysteresis > 0f)
            {
                //�Ķ� (��-����): near = rangedRange - rangeHysteresis (�ʹ� ������ ���� ���� ���)
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
                Gizmos.DrawWireSphere(p, profile.rangedRange - profile.rangeHysteresis);

                //�Ķ� (��-�ܺ�): far = rangedRange + rangeHysteresis (����� ǥ��)
                Gizmos.DrawWireSphere(p, profile.rangedRange + profile.rangeHysteresis);
            }

            //��(���ǿ�)
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