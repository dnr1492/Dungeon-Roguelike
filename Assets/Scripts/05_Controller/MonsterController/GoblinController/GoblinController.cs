using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoblinController : MonsterController
{
    [SerializeField] private GameObject targetingPrefab;

    private void OnDrawGizmos()
    {
        if (monsterBody == null) return;

        //인식 범위
        Gizmos.color = new Color(1, 1, 1, 0.2f);
        Gizmos.DrawSphere(monsterBody.transform.position, (float)CalRecognitionRange());

        //공격 범위
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawSphere(monsterBody.transform.position, (float)CalAttackRange());

        //left 히트 범위
        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawSphere(leftMeleeAttack.transform.position, attackRange);

        //right 히트 범위
        Gizmos.color = new Color(0, 0, 1, 0.2f);
        Gizmos.DrawSphere(rightMeleeAttack.transform.position, attackRange);
    }

    protected override void Init()
    {
        SetMonsterStadardStatus();
        SetCurMonsterName(MonsterName.Goblin);

        hp = maxHp;
        attackTimer = attackSpeed;
        
        ActivateTargeting(false);
    }

    protected override void Awake()
    {
        base.Awake();        
    }

    private void Update()
    {
        if (playerPivot == null) return;

        if (hp <= 0)
        {
            StartCoroutine(DieMonsterRoutine());
            ActivateTargeting(false);
            return;
        }

        if (CalDistance() <= CalAttackRange()) StartCoroutine(AttackPlayerRoutine());

        if (!isPossibleAttack) UpdateAttackTimer();
    }

    private void FixedUpdate()
    {
        if (playerPivot == null)
        {
            PatrolMonster();
            return;
        }

        if (hp <= 0) return;

        if (isAttacking) return;

        if (CalDistance() <= CalRecognitionRange() && CalDistance() > CalAttackRange()) MoveToPlayer();

        if (CalDistance() > CalRecognitionRange()) PatrolMonster();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PreventOverlap(collision);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Bullet")
        {
            if (!isDie)
            {
                gunController.DestroyOnHitBullet(collision.gameObject);
                HitMonster();
            }
        } 
        else if (collision.gameObject.tag == "Knife")
        {
            if (!isDie)
            {
                HitMonster();
                HitKnockback();
            }
        }
    }

    protected override void SetMonsterStadardStatus()
    {
        moveSpeed = 1f;
        recognitionRange = 1.15f;
        attackRange = 0.7f;
        attackSpeed = 5;
        damage = 1;
        maxHp = 4;
    }

    /// <summary>
    /// 현재 상호 작용하고 있는 몬스터의 이름을 가져오기
    /// </summary>
    /// <returns></returns>
    public override MonsterName GetCurMonsterName()
    {
        return curMonsterName;
    }

    /// <summary>
    /// 타겟팅 활성화 또는 비활성화
    /// </summary>
    /// <param name="active"></param>
    public void ActivateTargeting(bool active)
    {
        targetingPrefab.SetActive(active);
    }
}
