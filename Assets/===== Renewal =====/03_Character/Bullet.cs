using UnityEngine;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] CircleCollider2D col;

    //총알 상태
    private float speed;
    private float life;  //0이하면 무한
    private int damage;

    //풀 복귀 콜백
    private Action<Bullet> onDespawn;  

    public void Spawn(Vector3 pos, Quaternion rot, float spd, float lifeTime, float radius, int dmg, Action<Bullet> despawnCb)
    {
        transform.SetPositionAndRotation(pos, rot);
        speed = spd;
        life = lifeTime;
        damage = dmg;
        onDespawn = despawnCb;

        float sx = Mathf.Abs(transform.lossyScale.x);
        col.radius = (sx > 0f) ? (radius / sx) : radius;

        gameObject.SetActive(true);
    }

    void Update()
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

        //적: 데미지 적용 후 소멸
        if ((bit & ConstClass.Masks.Enemy) != 0)
        {
            if (other.TryGetComponent<Health>(out var hp)) hp.TakeDamage(damage);
            else
            {
                var p = other.transform.parent;
                if (p && p.TryGetComponent<Health>(out var php)) php.TakeDamage(damage);
            }
#if UNITY_EDITOR
            Debug.Log($"[Hit] {other.name} -{damage}");
#endif
            Despawn();
        }
    }

    private void Despawn()
    {
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
        if (!col) return;

        //발사체 시각화
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, col.radius);
    }
}
