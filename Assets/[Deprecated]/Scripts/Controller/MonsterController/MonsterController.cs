using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterController : Controller
{
    public enum MonsterName { None, Goblin }
    protected MonsterName curMonsterName;

    public int Damage { get { return damage; } /*set { damage = value; } */}
    //public int Hp { get { return hp; } }

    [Header("공통")]
    private Rigidbody2D curRigidbody2D;
    private SpriteRenderer curSpriteRenderer;
    private Animator monsterAni;
    protected Transform monsterBody;

    [Header("플레이어")]
    protected Transform playerPivot;

    [Header("몬스터 스탯")]
    protected float moveSpeed;  //스탯
    protected float recognitionRange;  //스탯
    protected float attackRange;  //스탯
    protected int attackSpeed;  //스탯
    protected int damage;  //스탯
    protected int hp;
    protected int maxHp;  //스탯
    
    [Header("몬스터 상태")]
    protected bool isAttacking = false;  //공격 중이면 true 아니면 false
    protected bool isPossibleAttack = true;  //공격이 가능하면 true 불가능하면 false
    protected bool isChecking = false;  //체크 중이면 true 아니면 false
    protected bool isDie = false;  //죽었으면 true 죽지 않았으면 false

    [Header("몬스터 이동")]
    private float moveTimer;
    private int moveDirectionX;
    private int moveDirectionY;
    private int moveStoping;

    [Header("몬스터 공격")]
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
    /// 몬스터가 배회(순찰)
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
    /// 몬스터 간 겹치는 것을 방지
    /// </summary>
    protected void PreventOverlap(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy") curRigidbody2D.bodyType = RigidbodyType2D.Kinematic;
        else curRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
    }

    /// <summary>
    /// 몬스터 인식 범위의 넓이를 계산
    /// </summary>
    /// <returns></returns>
    protected double CalRecognitionRange()
    {
        double circleRecognitionRange = recognitionRange * recognitionRange * 3.14;
        return circleRecognitionRange;
    }

    /// <summary>
    /// 몬스터 공격 범위의 넓이를 계산
    /// </summary>
    /// <returns></returns>
    protected double CalAttackRange()
    {
        double calCircleAttackRange = attackRange * attackRange * 3.14;
        return calCircleAttackRange;
    }

    /// <summary>
    /// 몬스터와 플레이어와의 거리를 계산
    /// </summary>
    protected float CalDistance()
    {
        float distance = Vector2.Distance(playerPivot.transform.position, transform.position);
        return distance;
    }

    /// <summary>
    /// 몬스터가 플레이어로 이동
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
    /// 몬스터가 플레이어를 공격
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
    /// 몬스터의 재공격 타이머
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
    /// 타겟이 존재하는지 체크
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
    /// 몬스터가 피격
    /// </summary>
    protected void HitMonster()
    {
        if (weaponController.GetCurWeaponType() == WeaponController.WeaponType.Gun) hp -= gunController.Damage;
        else if (weaponController.GetCurWeaponType() == WeaponController.WeaponType.Knife) hp -= knifeController.Damage;
        //Debug.LogFormat("몬스터가 {0}의 데미지를 받았습니다.", gunController.Damage);
        Debug.LogFormat("몬스터의 남은 체력은 {0} 입니다", hp);
    }

    /// <summary>
    /// 몬스터가 넉백
    /// </summary>
    protected void HitKnockback()
    {
        if (curSpriteRenderer.flipX == false) transform.position -= transform.right * 0.2f;
        else if (curSpriteRenderer.flipX == true) transform.position += transform.right * 0.2f;
    }

    /// <summary>
    /// 몬스터의 죽음
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
    /// 현재 상호 작용하고 있는 몬스터의 이름을 설정
    /// </summary>
    /// <param name="monsterName"></param>
    protected void SetCurMonsterName(MonsterName monsterName = MonsterName.None)
    {
        curMonsterName = monsterName;
    }

    /// <summary>
    /// 현재 상호 작용하고 있는 몬스터의 이름을 가져오기
    /// </summary>
    /// <returns></returns>
    public virtual MonsterName GetCurMonsterName()
    {
        return curMonsterName;
    }

    /// <summary>
    /// 몬스터의 스테이터스 기본 설정
    /// </summary>
    protected virtual void SetMonsterStadardStatus()
    {
    }
}