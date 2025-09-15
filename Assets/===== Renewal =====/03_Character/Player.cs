using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D rb;
    [SerializeField] SpriteRenderer sprite;  //캐릭터
    [SerializeField] Joystick joystick;
    [SerializeField] Transform weaponRoot;  //무기
    [SerializeField] Transform fireOrigin;  //총구/타격 기준점
    [SerializeField] Bullet bulletPrefab;

    public bool HasRoomBounds => hasBounds;
    public Bounds RoomBounds => currentRoomBounds;

    #region 이동
    private readonly float moveSpeed = 5f;     //이동 속도

    private Vector2 moveInput;
    #endregion

    #region 락온
    private readonly float stickyTime = 0.9f;  //락온 유지시간 (0이면 즉시 재선정)

    private bool isCombat;
    private Bounds currentRoomBounds;  //전투 중 스캔 범위
    private bool hasBounds;

    private Transform target;
    private float stickyUntil;
    #endregion

    #region 무기 발사
    //발사체 튜닝값
    private readonly float fireRate = 2f;      //초당 발사수
    private readonly int burst = 3;            //총 동시 발사수 (1 = 단발)
    private readonly float spreadDeg = 30f;    //총 산탄 각도 (버스트 > 1일 때만 의미)
    private readonly float bulletSpeed = 8f;   //투사체 속도
    private readonly float bulletLife = 1.2f;  //투사체 수명
    private readonly float bulletRad = 0.1f;   //충돌 반경
    private readonly int bulletDamage = 1;     //총알 데미지

    private float nextFireTime;

    private bool fireHeld;     //버튼을 누르고 있는 중
    private bool firePressed;  //이번 프레임에 "딱 한 번" 눌림 (edge)

    //총알 풀링
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
        //1) 이동
        Vector2 move = Vector2.zero;
        //조이스틱 우선
        if (joystick && joystick.Direction.sqrMagnitude > 0.0001f)
            move = joystick.Direction;
#if UNITY_EDITOR
        //에디터에서만 WASD 폴백
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

        //2) 전투 중이면 룸 Bounds로 락온 갱신
        if (isCombat && hasBounds) UpdateLockOn();

        //3) 무기 360도 회전 (타겟 우선, 없으면 이동방향)
        Vector2 aim = Vector2.zero;
        if (target) aim = (TargetPoint() - (Vector2)fireOrigin.position).normalized;
        else if (move.sqrMagnitude > 0.0001f) aim = move.normalized;
        if (weaponRoot && aim.sqrMagnitude > 0.0001f)
        {
            float z = Vector2.SignedAngle(Vector2.right, aim);
            weaponRoot.rotation = Quaternion.Euler(0, 0, z);
        }

        //4) 사격 입력
        //버튼으로만 발사, FireRate(쿨다운)으로 제한
        if (firePressed) { TryFire(); firePressed = false; }  //탭
        if (fireHeld) { TryFire(); }                          //홀드

        //5) 캐릭터는 회전하지 않고 FlipX만 변경
        float dirX = (aim.sqrMagnitude > 0.0001f) ? aim.x : move.x;
        if (sprite && Mathf.Abs(dirX) > 0.0001f) sprite.flipX = dirX < 0f;
    }

    private void FixedUpdate()
    {
        //이동
        if (!rb) return;
        rb.velocity = (moveInput.sqrMagnitude > 0.000001f)
            ? moveInput.normalized * moveSpeed
            : Vector2.zero;
    }

    #region 전투 세팅
    //전투 On/Off 및 현재 방 Bounds를 세팅
    public void SetCombat(bool on, Bounds roomBoundsWorld)
    {
        isCombat = on;
        hasBounds = on;
        currentRoomBounds = roomBoundsWorld;
        if (!on) { target = null; stickyUntil = 0f; }
    }

    //전투 Off 세팅
    public void SetCombat(bool on)
    {
        isCombat = on;
        if (!on) { hasBounds = false; target = null; stickyUntil = 0f; }
    }
    #endregion

    #region 룸 Bounds 기반 락온
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

        //방 내부 제한 (정밀)
        if (hasBounds && !currentRoomBounds.Contains(t.position)) return false;

        //시야(벽) 체크
        if (!HasLineOfSight(fireOrigin.position, t.position)) return false;

        return true;
    }

    private bool HasLineOfSight(Vector3 from3, Vector3 to3)
    {
        Vector2 from = from3;
        Vector2 to = to3;
        var dir = (to - from).normalized;
        var dist = Vector2.Distance(from, to);
        var hit = Physics2D.Raycast(from, dir, dist, ConstClass.Masks.Wall);  //"Wall" 레이어만 차단
        return hit.collider == null;
    }

    private Vector2 TargetPoint() => target ? target.position : transform.position;
    #endregion

    #region 무기 발사 (feat.풀링)
    //UI 바인딩용: 공격 버튼 Down
    public void FireButtonDown() { fireHeld = true; firePressed = true; }

    //UI 바인딩용: 공격 버튼 Up
    public void FireButtonUp() { fireHeld = false; }

    //발사
    private void TryFire()
    {
        if (Time.time < nextFireTime) return;
        float interval = 1f / Mathf.Max(0.0001f, fireRate);
        nextFireTime = Time.time + interval;

        //기준 방향 = 총구 방향 (무기 회전이 이미 조이스틱/타겟에 동기화되어 있다고 전제)
        float baseZ = fireOrigin ? fireOrigin.eulerAngles.z
                   : (weaponRoot ? weaponRoot.eulerAngles.z : 0f);

        int n = Mathf.Max(1, burst);
        float stepDeg = (n > 1) ? (spreadDeg / (n - 1)) : 0f;
        float startDeg = -spreadDeg * 0.5f;

        //스폰 위치 총구
        Vector3 spawnPos = fireOrigin ? fireOrigin.position : transform.position;

        for (int i = 0; i < n; i++)
        {
            float deg = (n == 1) ? 0f : (startDeg + stepDeg * i);
            float z = baseZ + deg;

            //풀에서 총알을 하나 꺼내서 세팅/발사  
            var proj = GetBullet();
            proj.Spawn(
                spawnPos,
                Quaternion.Euler(0f, 0f, z),
                bulletSpeed, bulletLife, bulletRad, bulletDamage,
                ConstClass.Masks.Enemy,                 //플레이어 총알은 적만 맞춤
                GetComponentsInChildren<Collider2D>(),  //내 콜라이더 전부 무시
                ReturnBullet
            );
        }
    }

    //총알을 풀링에다가 생성
    private void CreateBullet()
    {
        //Pool 루트 생성
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

    //총알을 풀링에서 가져오기
    private Bullet GetBullet()
    {
        if (bulletPool.Count > 0)
        {
            var b = bulletPool.Dequeue();
            b.transform.SetParent(null, true);  //월드로 꺼냄
            return b;
        }

        //부족할 경우 새로 생성
        return Instantiate(bulletPrefab);
    }

    //총알을 풀링에 반환
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
        //전투 중 스캔 범위 디버그
        if (hasBounds)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.25f);
            Gizmos.DrawCube(currentRoomBounds.center, currentRoomBounds.size);
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        //타겟 표시: 있을 때만
        if (target)
        {
            //타겟 발밑 링
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.35f);

            //총구 → 타겟 선
            Vector3 from = fireOrigin ? fireOrigin.position : transform.position;
            Debug.DrawLine(from, target.position, Color.red);
        }
    }
}