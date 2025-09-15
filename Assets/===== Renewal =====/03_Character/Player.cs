using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D rb;
    [SerializeField] SpriteRenderer sprite;  //ĳ����
    [SerializeField] Joystick joystick;
    [SerializeField] Transform weaponRoot;  //����
    [SerializeField] Transform fireOrigin;  //�ѱ�/Ÿ�� ������
    [SerializeField] Bullet bulletPrefab;

    public bool HasRoomBounds => hasBounds;
    public Bounds RoomBounds => currentRoomBounds;

    #region �̵�
    private readonly float moveSpeed = 5f;     //�̵� �ӵ�

    private Vector2 moveInput;
    #endregion

    #region ����
    private readonly float stickyTime = 0.9f;  //���� �����ð� (0�̸� ��� �缱��)

    private bool isCombat;
    private Bounds currentRoomBounds;  //���� �� ��ĵ ����
    private bool hasBounds;

    private Transform target;
    private float stickyUntil;
    #endregion

    #region ���� �߻�
    //�߻�ü Ʃ�װ�
    private readonly float fireRate = 2f;      //�ʴ� �߻��
    private readonly int burst = 3;            //�� ���� �߻�� (1 = �ܹ�)
    private readonly float spreadDeg = 30f;    //�� ��ź ���� (����Ʈ > 1�� ���� �ǹ�)
    private readonly float bulletSpeed = 8f;   //����ü �ӵ�
    private readonly float bulletLife = 1.2f;  //����ü ����
    private readonly float bulletRad = 0.1f;   //�浹 �ݰ�
    private readonly int bulletDamage = 1;     //�Ѿ� ������

    private float nextFireTime;

    private bool fireHeld;     //��ư�� ������ �ִ� ��
    private bool firePressed;  //�̹� �����ӿ� "�� �� ��" ���� (edge)

    //�Ѿ� Ǯ��
    private readonly int bulletPrewarm = 30;
    private readonly Queue<Bullet> bulletPool = new Queue<Bullet>(64);
    private Transform bulletPoolRoot;
    #endregion

    private void Awake()
    {
        CreateBullet();
    }

    private void Update()
    {
        //1) �̵�
        Vector2 move = Vector2.zero;
        //���̽�ƽ �켱
        if (joystick && joystick.Direction.sqrMagnitude > 0.0001f)
            move = joystick.Direction;
#if UNITY_EDITOR
        //�����Ϳ����� WASD ����
        else
        {
            float kx = 0f, ky = 0f;
            if (Input.GetKey(KeyCode.A)) kx -= 1f;
            if (Input.GetKey(KeyCode.D)) kx += 1f;
            if (Input.GetKey(KeyCode.S)) ky -= 1f;
            if (Input.GetKey(KeyCode.W)) ky += 1f;

            if (kx != 0f || ky != 0f)
                move = new Vector2(kx, ky);
        }
#endif
        moveInput = move;

        //2) ���� ���̸� �� Bounds�� ���� ����
        if (isCombat && hasBounds) UpdateLockOn();

        //3) ���� 360�� ȸ�� (Ÿ�� �켱, ������ �̵�����)
        Vector2 aim = Vector2.zero;
        if (target) aim = (TargetPoint() - (Vector2)fireOrigin.position).normalized;
        else if (move.sqrMagnitude > 0.0001f) aim = move.normalized;
        if (weaponRoot && aim.sqrMagnitude > 0.0001f)
        {
            float z = Vector2.SignedAngle(Vector2.right, aim);
            weaponRoot.rotation = Quaternion.Euler(0, 0, z);
        }

        //4) ��� �Է�
        //��ư���θ� �߻�, FireRate(��ٿ�)���� ����
        if (firePressed) { TryFire(); firePressed = false; }  //��
        if (fireHeld) { TryFire(); }                          //Ȧ��

        //5) ĳ���ʹ� ȸ������ �ʰ� FlipX�� ����
        float dirX = (aim.sqrMagnitude > 0.0001f) ? aim.x : move.x;
        if (sprite && Mathf.Abs(dirX) > 0.0001f) sprite.flipX = dirX < 0f;
    }

    private void FixedUpdate()
    {
        //�̵�
        if (!rb) return;
        rb.velocity = (moveInput.sqrMagnitude > 0.000001f)
            ? moveInput.normalized * moveSpeed
            : Vector2.zero;
    }

    #region ���� ����
    //���� On/Off �� ���� �� Bounds�� ����
    public void SetCombat(bool on, Bounds roomBoundsWorld)
    {
        isCombat = on;
        hasBounds = on;
        currentRoomBounds = roomBoundsWorld;
        if (!on) { target = null; stickyUntil = 0f; }
    }

    //���� Off ����
    public void SetCombat(bool on)
    {
        isCombat = on;
        if (!on) { hasBounds = false; target = null; stickyUntil = 0f; }
    }
    #endregion

    #region �� Bounds ��� ����
    private void UpdateLockOn()
    {
        if (target && Time.time <= stickyUntil && IsValidTarget(target))
            return;

        Vector2 c = currentRoomBounds.center;
        Vector2 s = currentRoomBounds.size;
        var hits = Physics2D.OverlapBoxAll(c, s, 0f, ConstClass.Masks.Enemy);

        float best = float.MaxValue;
        Transform bestT = null;
        Vector2 origin = fireOrigin ? fireOrigin.position : transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            if (!IsValidTarget(t)) continue;

            float d = ((Vector2)t.position - origin).sqrMagnitude;
            if (d < best) { best = d; bestT = t; }
        }

        target = bestT;
        if (target && stickyTime > 0f) stickyUntil = Time.time + stickyTime;
    }

    private bool IsValidTarget(Transform t)
    {
        if (!t) return false;

        //�� ���� ���� (����)
        if (hasBounds && !currentRoomBounds.Contains(t.position)) return false;

        //�þ�(��) üũ
        if (!HasLineOfSight(fireOrigin.position, t.position)) return false;

        return true;
    }

    private bool HasLineOfSight(Vector3 from3, Vector3 to3)
    {
        Vector2 from = from3;
        Vector2 to = to3;
        var dir = (to - from).normalized;
        var dist = Vector2.Distance(from, to);
        var hit = Physics2D.Raycast(from, dir, dist, ConstClass.Masks.Wall);  //"Wall" ���̾ ����
        return hit.collider == null;
    }

    private Vector2 TargetPoint() => target ? target.position : transform.position;
    #endregion

    #region ���� �߻� (feat.Ǯ��)
    //UI ���ε���: ���� ��ư Down
    public void FireButtonDown() { fireHeld = true; firePressed = true; }

    //UI ���ε���: ���� ��ư Up
    public void FireButtonUp() { fireHeld = false; }

    //�߻�
    private void TryFire()
    {
        if (Time.time < nextFireTime) return;
        float interval = 1f / Mathf.Max(0.0001f, fireRate);
        nextFireTime = Time.time + interval;

        //���� ���� = �ѱ� ���� (���� ȸ���� �̹� ���̽�ƽ/Ÿ�ٿ� ����ȭ�Ǿ� �ִٰ� ����)
        float baseZ = fireOrigin ? fireOrigin.eulerAngles.z
                   : (weaponRoot ? weaponRoot.eulerAngles.z : 0f);

        int n = Mathf.Max(1, burst);
        float stepDeg = (n > 1) ? (spreadDeg / (n - 1)) : 0f;
        float startDeg = -spreadDeg * 0.5f;

        //���� ��ġ �ѱ�
        Vector3 spawnPos = fireOrigin ? fireOrigin.position : transform.position;

        for (int i = 0; i < n; i++)
        {
            float deg = (n == 1) ? 0f : (startDeg + stepDeg * i);
            float z = baseZ + deg;

            //Ǯ���� �Ѿ��� �ϳ� ������ ����/�߻�  
            var proj = GetBullet();
            proj.Spawn(
                spawnPos,
                Quaternion.Euler(0f, 0f, z),
                bulletSpeed, bulletLife, bulletRad, bulletDamage,
                ConstClass.Masks.Enemy,                 //�÷��̾� �Ѿ��� ���� ����
                GetComponentsInChildren<Collider2D>(),  //�� �ݶ��̴� ���� ����
                ReturnBullet
            );
        }
    }

    //�Ѿ��� Ǯ�����ٰ� ����
    private void CreateBullet()
    {
        //Pool ��Ʈ ����
        var root = new GameObject("Pool_Bullets");
        bulletPoolRoot = root.transform;
        bulletPoolRoot.SetParent(transform.root, false);

        for (int i = 0; i < bulletPrewarm; i++)
        {
            var b = Instantiate(bulletPrefab, bulletPoolRoot);
            b.gameObject.SetActive(false);
            bulletPool.Enqueue(b);
        }
    }

    //�Ѿ��� Ǯ������ ��������
    private Bullet GetBullet()
    {
        if (bulletPool.Count > 0)
        {
            var b = bulletPool.Dequeue();
            b.transform.SetParent(null, true);  //����� ����
            return b;
        }

        //������ ��� ���� ����
        return Instantiate(bulletPrefab);
    }

    //�Ѿ��� Ǯ���� ��ȯ
    private void ReturnBullet(Bullet b)
    {
        if (!b) return;

        b.gameObject.SetActive(false);
        b.transform.SetParent(bulletPoolRoot, true);
        bulletPool.Enqueue(b);
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        //���� �� ��ĵ ���� �����
        if (hasBounds)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.25f);
            Gizmos.DrawCube(currentRoomBounds.center, currentRoomBounds.size);
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        //Ÿ�� ǥ��: ���� ����
        if (target)
        {
            //Ÿ�� �߹� ��
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.35f);

            //�ѱ� �� Ÿ�� ��
            Vector3 from = fireOrigin ? fireOrigin.position : transform.position;
            Debug.DrawLine(from, target.position, Color.red);
        }
    }
}