using UnityEngine;
using System;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    [SerializeField] CircleCollider2D circleCol;

    //�Ѿ� ����
    private float speed;
    private float life;  //0���ϸ� ����
    private int damage;

    //Ǯ ���� �ݹ�
    private Action<Bullet> onDespawn;

    private LayerMask hitMask;  //�Ѿ��� ���� ��� (Player �Ǵ� Enemy)
    private readonly List<Collider2D> ignored = new();

    public void Spawn(
        Vector3 pos, Quaternion rot,
        float spd, float lifeTime, float radius, int dmg,
        LayerMask hitMask,
        Collider2D[] ignoreColliders,  //�߻��� �ݶ��̴���
        Action<Bullet> despawnCb)
    {
        //�ѱ����� �߻�
        transform.SetPositionAndRotation(pos, rot);

        speed = spd;
        life = lifeTime;
        damage = dmg;
        onDespawn = despawnCb;
        this.hitMask = hitMask;

        //�ݰ� ����
        float sx = Mathf.Abs(transform.lossyScale.x);
        circleCol.radius = (sx > 0f) ? (radius / sx) : radius;

        //�߻��ڿ� �浹 ����
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

        //��: ��� �Ҹ�
        if ((bit & ConstClass.Masks.Wall) != 0)
        {
            Despawn();
            return;
        }

        //���� ���: ������ ���� �� �Ҹ�
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
        //�����ߴ� �浹 ����
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
        //������� �ڵ�: ��Ȱ��ȭ�� �� �̵�/���� ���� �ʱ�ȭ (���� �� ����)
        //life/damage�� Spawn���� �Ź� �ʱ�ȭ
        speed = 0f;
    }

    private void OnDrawGizmos()
    {
        if (!circleCol) return;

        //�߻�ü �ð�ȭ
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, circleCol.radius);
    }
}
