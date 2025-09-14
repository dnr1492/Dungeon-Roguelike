using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] CircleCollider2D col;

    private float speed;
    private float life;
    private int damage;

    public void Init(float spd, float lifeTime, int dmg, float radius)
    {
        speed = spd;
        life = lifeTime;
        damage = dmg;
        col.radius = radius;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        transform.Translate(dt * speed * Vector3.right, Space.Self);

        life -= dt;
        if (life <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        int bit = 1 << other.gameObject.layer;

        //��: ��� �Ҹ�
        if ((bit & ConstClass.Masks.Wall) != 0)
        {
            Destroy(gameObject);
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
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (!col) return;

        //�߻�ü �ð�ȭ
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, col.radius);
    }
}
