using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : Controller
{
    public enum MonsterName { None, Goblin }
    protected MonsterName curMonsterName;

    public int Damage { get { return damage; } /*set { damage = value; } */}
    //public int Hp { get { return hp; } }

    [Header("����")]
    private Rigidbody2D curRigidbody2D;
    private SpriteRenderer curSpriteRenderer;
    private Animator monsterAni;
    protected Transform monsterBody;

    [Header("�÷��̾�")]
    protected Transform playerPivot;

    [Header("���� ����")]
    protected float moveSpeed;  //����
    protected float recognitionRange;  //����
    protected float attackRange;  //����
    protected int attackSpeed;  //����
    protected int damage;  //����
    protected int hp;
    protected int maxHp;  //����
    
    [Header("���� ����")]
    protected bool isAttacking = false;  //���� ���̸� true �ƴϸ� false
    protected bool isPossibleAttack = true;  //������ �����ϸ� true �Ұ����ϸ� false
    protected bool isChecking = false;  //üũ ���̸� true �ƴϸ� false
    protected bool isDie = false;  //�׾����� true ���� �ʾ����� false

    [Header("���� �̵�")]
    private float moveTimer;
    private int moveDirectionX;
    private int moveDirectionY;
    private int moveStoping;

    [Header("���� ����")]
    [SerializeField] protected Transform leftMeleeAttack, rightMeleeAttack;
    protected float attackTimer = 0;

    protected virtual void Init()
    {
    }

    protected override void Awake()
    {
        base.Awake();

        curRigidbody2D = gameObject.GetComponent<Rigidbody2D>();
        curSpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        monsterAni = gameObject.GetComponent<Animator>();

        monsterBody = GameObject.Find("MonsterBody").transform;
        playerPivot = GameObject.Find("PlayerPivot").transform;

        SetSelectedPlayer();

        Init();
    }

    /// <summary>
    /// ���Ͱ� ��ȸ(����)
    /// </summary>
    protected void PatrolMonster()
    {
        if (moveTimer > 0)
        {
            if (moveDirectionX == 0 && moveDirectionY == 0 || moveStoping != 0)
            {
                monsterAni.Play("Idle");
                curRigidbody2D.velocity = Vector3.zero;
            }
            else
            {
                monsterAni.Play("Run");
                curRigidbody2D.velocity = new Vector2(moveDirectionX, moveDirectionY) * moveSpeed;
            }
            moveTimer -= Time.deltaTime;
        }
        else
        {
            moveTimer = Random.Range(2, 7);  //2~6
            moveDirectionX = Random.Range(-1, 2);  //-1~1
            moveDirectionY = Random.Range(-1, 2);  //-1~1
            moveStoping = Random.Range(0, 3);  //0~2
        }

        if (moveDirectionX == -1) curSpriteRenderer.flipX = true;
        else if (moveDirectionX == 1) curSpriteRenderer.flipX = false;
    }

    /// <summary>
    /// ���� �� ��ġ�� ���� ����
    /// </summary>
    protected void PreventOverlap(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy") curRigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        else curRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
    }

    /// <summary>
    /// ���� �ν� ������ ���̸� ���
    /// </summary>
    /// <returns></returns>
    protected double CalRecognitionRange()
    {
        double circleRecognitionRange = recognitionRange * recognitionRange * 3.14;
        return circleRecognitionRange;
    }

    /// <summary>
    /// ���� ���� ������ ���̸� ���
    /// </summary>
    /// <returns></returns>
    protected double CalAttackRange()
    {
        double calCircleAttackRange = attackRange * attackRange * 3.14;
        return calCircleAttackRange;
    }

    /// <summary>
    /// ���Ϳ� �÷��̾���� �Ÿ��� ���
    /// </summary>
    protected float CalDistance()
    {
        float distance = Vector2.Distance(playerPivot.transform.position, transform.position);
        return distance;
    }

    /// <summary>
    /// ���Ͱ� �÷��̾�� �̵�
    /// </summary>
    protected void MoveToPlayer()
    {
        Rigidbody2D targetRigidbody2D = selectedPlayer.GetComponent<Rigidbody2D>();
        Vector2 dirVec = targetRigidbody2D.position - curRigidbody2D.position;
        Vector2 nextVec = dirVec.normalized * moveSpeed * Time.deltaTime;
        curRigidbody2D.MovePosition(curRigidbody2D.position + nextVec);
        curRigidbody2D.velocity = Vector2.zero;
        curSpriteRenderer.flipX = targetRigidbody2D.position.x < curRigidbody2D.position.x;
        monsterAni.Play("Run");
    }

    /// <summary>
    /// ���Ͱ� �÷��̾ ����
    /// </summary>
    /// <returns></returns>
    protected IEnumerator AttackPlayerRoutine()
    {
        if (isPossibleAttack)
        {
            isAttacking = true;
            monsterAni.Play("Attack");
            yield return new WaitForSeconds(0.5f);

            Collider2D target = CheckTarget();
            if (target != null) target.GetComponentInParent<CharacterController>().HitPlayer();
            yield return new WaitForSeconds(0.2f);

            isAttacking = false;
            isPossibleAttack = false;
        }
        else if (!isPossibleAttack) monsterAni.Play("Idle");
    }

    /// <summary>
    /// ������ ����� Ÿ�̸�
    /// </summary>
    protected void UpdateAttackTimer()
    {
        if (attackTimer <= 0)
        {
            if (!isPossibleAttack) attackTimer = attackSpeed;
            isPossibleAttack = true;
            isChecking = false;
            return;
        }
        attackTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Ÿ���� �����ϴ��� üũ
    /// </summary>
    private Collider2D CheckTarget()
    {
        if (isChecking) return null;
        isChecking = true;

        if (curSpriteRenderer.flipX)
        {
            leftMeleeAttack.gameObject.SetActive(true);
            rightMeleeAttack.gameObject.SetActive(false);
        }
        else if (!curSpriteRenderer.flipX)
        {
            leftMeleeAttack.gameObject.SetActive(false);
            rightMeleeAttack.gameObject.SetActive(true);
        }

        Collider2D target = null;
        if (leftMeleeAttack.gameObject.activeSelf) target = Physics2D.OverlapCircle(leftMeleeAttack.position, attackRange, LayerMask.GetMask("PlayerBody"));
        else if (rightMeleeAttack.gameObject.activeSelf) target = Physics2D.OverlapCircle(rightMeleeAttack.position, attackRange, LayerMask.GetMask("PlayerBody"));
        return target;
    }

    /// <summary>
    /// ���Ͱ� �ǰ�
    /// </summary>
    protected void HitMonster()
    {
        if (weaponController.GetCurWeaponType() == WeaponController.WeaponType.Gun) hp -= gunController.Damage;
        else if (weaponController.GetCurWeaponType() == WeaponController.WeaponType.Knife) hp -= knifeController.Damage;
        //Debug.LogFormat("���Ͱ� {0}�� �������� �޾ҽ��ϴ�.", gunController.Damage);
        Debug.LogFormat("������ ���� ü���� {0} �Դϴ�", hp);
    }

    /// <summary>
    /// ���Ͱ� �˹�
    /// </summary>
    protected void HitKnockback()
    {
        if (curSpriteRenderer.flipX == false) transform.position -= transform.right * 0.2f;
        else if (curSpriteRenderer.flipX == true) transform.position += transform.right * 0.2f;
    }

    /// <summary>
    /// ������ ����
    /// </summary>
    /// <returns></returns>
    public IEnumerator DieMonsterRoutine()
    {
        if (isDie) yield break;
        isDie = true;
        monsterAni.Play("Die");
        gameObject.GetComponentInChildren<BoxCollider2D>().gameObject.SetActive(false);
        yield return new WaitForSeconds(4f);

        Destroy(gameObject);
    }

    /// <summary>
    /// ���� ��ȣ �ۿ��ϰ� �ִ� ������ �̸��� ����
    /// </summary>
    /// <param name="monsterName"></param>
    protected void SetCurMonsterName(MonsterName monsterName = MonsterName.None)
    {
        curMonsterName = monsterName;
    }

    /// <summary>
    /// ���� ��ȣ �ۿ��ϰ� �ִ� ������ �̸��� ��������
    /// </summary>
    /// <returns></returns>
    public virtual MonsterName GetCurMonsterName()
    {
        return curMonsterName;
    }

    /// <summary>
    /// ������ �������ͽ� �⺻ ����
    /// </summary>
    protected virtual void SetMonsterStadardStatus()
    {
    }
}