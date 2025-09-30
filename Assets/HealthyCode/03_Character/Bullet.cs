using UnityEngine;
using System;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    [SerializeField] CircleCollider2D circleCol;

    //총알 상태
    private float speed;
    private float life;  //0이하면 무한
    private int damage;

    //풀 복귀 콜백
    private Action<Bullet> onDespawn;

    private LayerMask hitMask;  //총알이 맞출 대상 (Player 또는 Enemy)
    private readonly List<Collider2D> ignored = new();

    public void Spawn(
        Vector3 pos, Quaternion rot,
        float spd, float lifeTime, float radius, int dmg,
        LayerMask hitMask,
        Collider2D[] ignoreColliders,  //발사자 콜라이더들
        Action<Bullet> despawnCb)
    {
        //총구에서 발사
        transform.SetPositionAndRotation(pos, rot);

        speed = spd;
        life = lifeTime;
        damage = dmg;
        onDespawn = despawnCb;
        this.hitMask = hitMask;

        //반경 세팅
        float sx = Mathf.Abs(transform.lossyScale.x);
        circleCol.radius = (sx > 0f) ? (radius / sx) : radius;

        //발사자와 충돌 무시
        ignored.Clear();
        if (ignoreColliders != null)
        {
            for (int i = 0; i < ignoreColliders.Length; i++)
            {
                var oc = ignoreColliders[i];
                if (!oc || oc == circleCol) continue;
                Physics2D.IgnoreCollision(circleCol, oc, true);
                ignored.Add(oc);
            }
        }

        gameObject.SetActive(true);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        transform.Translate(Vector3.right * speed * dt, Space.Self);

        if (life > 0f)
        {
            life -= dt;
            if (life <= 0f) Despawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int bit = 1 << other.gameObject.layer;

        //벽: 즉시 소멸
        if ((bit & ConstClass.Masks.Wall) != 0)
        {
            Despawn();
            return;
        }

        //맞출 대상: 데미지 적용 후 소멸
        if ((bit & hitMask) != 0)
        {
            if (other.TryGetComponent<Health>(out var hp)) hp.TakeDamage(damage);
            else
            {
                var p = other.transform.parent;
                if (p && p.TryGetComponent<Health>(out var php)) php.TakeDamage(damage);
            }
            Despawn();

#if UNITY_EDITOR
            Debug.Log($"[Hit] {other.name} -{damage}");
#endif
        }
    }

    private void Despawn()
    {
        //무시했던 충돌 원복
        for (int i = 0; i < ignored.Count; i++)
        {
            var oc = ignored[i];
            if (oc) Physics2D.IgnoreCollision(circleCol, oc, false);
        }
        ignored.Clear();

        var cb = onDespawn;
        onDespawn = null;
        if (cb != null) cb(this);
        else gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        //방어적인 코드: 비활성화될 때 이동/수명 상태 초기화 (재사용 시 안정)
        //life/damage는 Spawn에서 매번 초기화
        speed = 0f;
    }

    private void OnDrawGizmos()
    {
        if (!circleCol) return;

        //발사체 시각화
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, circleCol.radius);
    }
}
