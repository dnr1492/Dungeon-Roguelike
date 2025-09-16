using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform fireOrigin;  //���Ÿ� ��� �� �ѱ� (������ transform)
    
    private readonly float moveSpeed = 2.5f;

    private Transform target;  //Player Transform
    private int attackerId;  //GetInstanceID()
    private bool encounterActive = false;  //�� ���� Ȱ�� ����

    #region ���� ���� (���� ������)
    [Header("Melee Attack")]
    [SerializeField] bool enableMelee = true;
    [SerializeField] Collider2D meleeTrigger;

    private readonly int meleeDamage = 1;
    private readonly float meleeRange = 1f;    //���� ���� ��Ÿ� (Ʈ���� ��� ����)
    private readonly float tickInterval = 0.4f;  //���� �� �� ���� ��� Ÿ�� ����
    private readonly float knockback = 3f;

    //�ش� ��(�÷��̾�) ���� ���� ƽ ��ٿ�
    private readonly Dictionary<int, float> lastHit = new Dictionary<int, float>();
    #endregion

    #region ���Ÿ� ���� (�߻�ü ���)
    [Header("Ranged Attack")]
    [SerializeField] bool enableRanged = false;
    [SerializeField] Bullet bulletPrefab;

    private readonly float fireRate = 0.2f;      //�ʴ� �߻��
    private readonly int burst = 1;              //�� ���� �߻�� (1 = �ܹ�)
    private readonly float spreadDeg = 0f;       //�� ��ź ���� (����Ʈ > 1�� ���� �ǹ�)
    private readonly float bulletSpeed = 7f;     //����ü �ӵ�
    private readonly float bulletLife = 2.0f;    //����ü ����
    private readonly float bulletRadius = 0.1f;  //�浹 �ݰ�
    private readonly int bulletDamage = 1;       //�Ѿ� ������
    private readonly float rangedRange = 5f;     //���Ÿ� ���� ��Ÿ�
    private readonly float meleeExitRange = 3f;  //���� ���� ���� ���� �Ÿ� (meleeRange ���� Ŭ ��)

    private bool meleeEngaged = false;                   //���̺긮�� ���� ���� ���� ����
    private readonly float rangeHysteresis = 0.35f;      //��� ���� ���� ����
    private readonly bool backpedalWhenTooClose = true;  //���Ÿ�: �ʹ� ������ �ڷ� ��¦ ������ (ī����)
    private float nextFireTime;
    #endregion

    #region ����Ʈ �и� (��ħ �ּ�ȭ)
    private readonly float separationRadius = 0.6f;    //���� �ʹ� ������ �о �ݰ�
    private readonly float separationStrength = 0.5f;  //�и� ����ġ
    private readonly Collider2D[] sepBuf = new Collider2D[8];  //GC ������ ����
    #endregion

    private void Awake()
    {
        attackerId = GetInstanceID();

        //Ÿ�� �ڵ� Ž��
        var go = GameObject.FindGameObjectWithTag(ConstClass.Tags.Player);
        if (go) target = go.transform;

        //���� Ʈ���Ű� �ִٸ� �ݵ�� isTrigger = true
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

        // ���� �̵� ��å ��������������������������������������������������������������������������������������������������������������������������
        // 1) ���̺긮��: '���� ���� ��Ÿ� ��' && '���� ���� ���� ���� �Ÿ� ��' �̸� '���� ����' ����, �ٱ��̸� '���Ÿ� ����' ���� (��Ÿ� ����)
        // 2) ���� ���� (enableMelee�� On): meleeRange �̳��� ��� ����
        // 3) ���Ÿ� ���� (enableRanged�� On): ��Ÿ� ���� (�ָ� ����, �ʹ� ������ ���� or ����)
        // ������������������������������������������������������������������������������������������������������������������������������������������

        if (enableMelee && enableRanged)
        {
            //����: ����� ������ ������ ���� ���� ����
            if (meleeEngaged)
            {
                if (dist > meleeExitRange) meleeEngaged = false;
                rb.velocity = (dist <= meleeRange) 
                    ? Vector2.zero      //���� ���� ��Ÿ� ���� �� ���� (Ʈ���� Ÿ��)
                    : moveSpeed * dir;  //�� ������ ��� ���� (ī���� ����)
            }
            else
            {
                //���� ��ȯ
                if (dist <= meleeExitRange)
                {
                    if (dist <= meleeRange)
                    {
                        meleeEngaged = true;
                        rb.velocity = Vector2.zero;
                    }
                    else
                    {
                        rb.velocity = moveSpeed * dir;  //�پ ���� ����
                    }
                }
                //���Ÿ� ���� ����
                else
                {
                    float near = rangedRange - rangeHysteresis;  //�ʹ� ��������� ���� ���� ���
                    if (dist > rangedRange) rb.velocity = moveSpeed * dir;  //��Ÿ� �ٱ� �� ��� ����
                    else if (dist < near) rb.velocity = backpedalWhenTooClose ? -moveSpeed * dir : Vector2.zero;  //�ʹ� ����� �� ����
                    else rb.velocity = Vector2.zero;  //��Ÿ� ���� �� ���� (���)
                }
            }
        }
        //���� ���� ����
        else if (enableMelee)
        {
            rb.velocity = (dist <= meleeRange) ? Vector2.zero : moveSpeed * dir;
        }
        //���Ÿ� ���� ����
        else if (enableRanged)
        {
            float near = rangedRange - rangeHysteresis;  //�ʹ� ��������� ���� ���� ���
            if (dist > rangedRange) rb.velocity = moveSpeed * dir;  //��Ÿ� �ٱ� �� ��� ����
            else if (dist < near) rb.velocity = backpedalWhenTooClose ? -moveSpeed * dir : Vector2.zero;  //�ʹ� ����� �� ����
            else rb.velocity = Vector2.zero;  //��Ÿ� ���� �� ���� (���)
        }
        else
        {
            rb.velocity = moveSpeed * dir;
        }

        //����Ʈ �и� (�� ��ħ �ּ�ȭ): ����� Enemy���� ��¦ �о��.
        //���� �߿��� ���� (��ħ �ּ�ȭ)
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
                    float w = Mathf.InverseLerp(separationRadius, 0f, d);  //�������� ���ߡ�
                    push += delta.normalized * w;
                }
                if (push.sqrMagnitude > 0.0001f)
                {
                    Vector2 sep = push.normalized * separationStrength;
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
        if (!enableRanged || !bulletPrefab) return;

        //���̺긮�忡�� ���� ���� ���� �߿��� ���Ÿ� ���� ����
        if (enableMelee && enableRanged && meleeEngaged) return;

        float dist = Vector2.Distance(transform.position, target.position);

        //���Ÿ�: '���� ��Ÿ� ��'������ �߻� (��ٿ� ����)
        if (dist <= rangedRange && Time.time >= nextFireTime)
        {
            FireAt(target.position);
            nextFireTime = Time.time + 1f / Mathf.Max(0.0001f, fireRate);
        }
    }

    //���� ������ ��쿡�� ȣ��
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!encounterActive) return;
        if (!enableMelee) return;
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
        if (lastHit.TryGetValue(targetId, out var last) && now - last < tickInterval) return;
        lastHit[targetId] = now;

        //���� ������: ������ ID�� �Ѱ� '�����ں� i-frame' ����
        //�÷��̾� Health���� usePerSourceIFrame = true�� ���� �ǹ̰� ����
        h.TakeHit(meleeDamage, attackerId);

        //�˹�
        if (knockback > 0f && h.TryGetComponent<Rigidbody2D>(out var prb))
        {
            Vector2 dir = (other.bounds.center - meleeTrigger.bounds.center);
            if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
            prb.AddForce(dir * knockback, ForceMode2D.Impulse);
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
    }

    #region ���� �߻�
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
        bool drawMelee = enableMelee;
        bool drawRanged = enableRanged;

        Vector3 p = transform.position;

        if (drawMelee)
        {
            //����: meleeRange (���� Ÿ���� ������ ���� �ݰ�)
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.55f);
            Gizmos.DrawWireSphere(p, meleeRange);

            //��Ȳ: meleeExitRange (���� ���� ���� / ���̺긮�� ��ȯ �ܰ� ���)
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.30f);
            Gizmos.DrawWireSphere(p, meleeExitRange);

            //�� (���ǿ�)
            UnityEditor.Handles.color = new Color(1f, 1f, 1f, 0.8f);
            UnityEditor.Handles.Label(p + Vector3.right * (meleeRange + 0.05f), "meleeRange");
            UnityEditor.Handles.Label(p + Vector3.right * (meleeExitRange + 0.05f), "meleeExitRange");
        }

        if (drawRanged && rangedRange > 0f)
        {
            //�Ķ� (��): rangedRange (���Ÿ� ���� ��Ÿ�)
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.85f);
            Gizmos.DrawWireSphere(p, rangedRange);

            if (rangeHysteresis > 0f)
            {
                //�Ķ� (��-����): near = rangedRange - rangeHysteresis (�ʹ� ������ ���� ���� ���)
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
                Gizmos.DrawWireSphere(p, rangedRange - rangeHysteresis);

                //�Ķ� (��-�ܺ�): far = rangedRange + rangeHysteresis (����� ǥ��)
                Gizmos.DrawWireSphere(p, rangedRange + rangeHysteresis);
            }

            //��(���ǿ�)
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