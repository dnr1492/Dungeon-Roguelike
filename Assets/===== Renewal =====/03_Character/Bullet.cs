using UnityEngine;
using System;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] CircleCollider2D col;

    //�Ѿ� ����
    private float speed;
    private float life;  //0���ϸ� ����
    private int damage;

    //Ǯ ���� �ݹ�
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

        //��: ��� �Ҹ�
        if ((bit & ConstClass.Masks.Wall) != 0)
        {
            Despawn();
            return;
        }

        //��: ������ ���� �� �Ҹ�
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
        //������� �ڵ�: ��Ȱ��ȭ�� �� �̵�/���� ���� �ʱ�ȭ (���� �� ����)
        //life/damage�� Spawn���� �Ź� �ʱ�ȭ
        speed = 0f;
    }

    private void OnDrawGizmos()
    {
        if (!col) return;

        //�߻�ü �ð�ȭ
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, col.radius);
    }
}
