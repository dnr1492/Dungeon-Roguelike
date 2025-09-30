using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : PlayerController
{
    //public int Hp { get { return hp; } }
    //public int Mp { get { return mp; } }

    [Header("플레이어 스탯")]
    protected float moveSpeed;  //스탯
    protected int hp;
    protected int maxHp;  //스탯
    protected int mp;
    protected int maxMp;  //스탯
    
    [Header("플레이어 상태")]
    protected bool isDie = false;  //죽었으면 true 죽지 않았으면 false

    [Header("타겟팅")]
    private bool isTargeting = false;  //타겟팅 중이면 true 아니면 false
    private bool isTargetingDir = false;  //타겟팅 위치가 플레이어 기준 오른쪽이면 true 왼쪽이면 false
    protected float outoTargetingRange = 5f;
    private float outoTargetingDistance = 25f;
    private float targetingAngle;

    protected override void Init()
    {
    }

    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// 조이스틱으로 캐릭터의 이동
    /// </summary>
    protected void MovePlayer()
    {
        if (curRigidbody2D.velocity != Vector2.zero) curRigidbody2D.velocity = Vector2.zero;

        Vector3 rightMovement = Vector3.right * moveSpeed * Time.deltaTime * joystick.Horizontal;
        Vector3 upMovement = Vector3.up * moveSpeed * Time.deltaTime * joystick.Vertical;
        transform.position += upMovement;
        transform.position += rightMovement;

        if (joystick.Horizontal != 0 || joystick.Vertical != 0)
        {
            playerAni.Play("Run");

            if (isTargeting) return;

            if (joystick.Horizontal > 0) curSpriteRenderer.flipX = false;
            else curSpriteRenderer.flipX = true;
        }
        else
        {
            if (playerAni.GetCurrentAnimatorStateInfo(0).IsName("Idle") == false) playerAni.Play("Idle");
        }
    }

    /// <summary>
    /// 플레이어가 피격
    /// </summary>
    public void HitPlayer()
    {
        if (goblinController.GetCurMonsterName() == MonsterController.MonsterName.Goblin)
        {
            hp -= goblinController.Damage;
            Debug.LogFormat("플레이어가 {0}의 데미지를 받았습니다.", goblinController.Damage);
            Debug.LogFormat("플레이어의 남은 체력은 {0} 입니다", hp);
        }
    }

    /// <summary>
    /// 플레이어의 죽음
    /// </summary>
    /// <returns></returns>
    public IEnumerator DiePlayerRoutine()
    {
        if (isDie) yield break;
        isDie = true;
        playerAni.Play("Die");
        weaponController.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.3f);

        Destroy(gameObject);
        isDie = false;
    }

    /// <summary>
    /// 가장 가까운 적을 타겟팅
    /// </summary>
    protected void FindTargeting()
    {
        Collider2D[] targets = Physics2D.OverlapCircleAll(knightController.transform.position, outoTargetingRange, LayerMask.GetMask("MonsterBody"));
        if (targets.Length == 0)
        {
            isTargeting = false;
            return;
        }

        float shortDis = (targets[0].transform.position - transform.position).sqrMagnitude;

        foreach (var target in targets)
        {
            float curDis = (target.transform.position - transform.position).sqrMagnitude;
            if (curDis <= shortDis)
            {
                targets[0].GetComponentInParent<GoblinController>().ActivateTargeting(false);
                targets[0] = target;
                shortDis = curDis;
            }

            if (shortDis <= outoTargetingDistance)
            {
                targets[0].GetComponentInParent<GoblinController>().ActivateTargeting(true);
                isTargeting = true;

                RotatePlayerToTargeting(targets[0]);
                CalRotationToTargeting(targets[0]);
            }
            else
            {
                targets[0].GetComponentInParent<GoblinController>().ActivateTargeting(false);
                isTargeting = false;
            }
        }
    }

    /// <summary>
    /// 플레이어가 타겟팅한 적을 바라보기
    /// </summary>
    /// <param name="target"></param>
    private void RotatePlayerToTargeting(Collider2D target)
    {
        if (target.transform.position.x > transform.position.x)
        {
            curSpriteRenderer.flipX = false;
            isTargetingDir = true;
        }
        else
        {
            curSpriteRenderer.flipX = true;
            isTargetingDir = false;
        }
    }

    /// <summary>
    /// 가장 가까운 적을 타겟팅하기 위한 각도를 계산
    /// </summary>
    /// <param name="target"></param>
    protected void CalRotationToTargeting(Collider2D target)
    {
        float x = target.transform.position.x - GetComponentInChildren<BoxCollider2D>().transform.position.x;
        float y = target.transform.position.y - GetComponentInChildren<BoxCollider2D>().transform.position.y;
        targetingAngle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// 가장 가까운 적을 타겟팅하기 위해서 계산한 각도를 가져오기
    /// </summary>
    /// <returns></returns>
    public float GetTargetingAngle()
    {
        return targetingAngle;
    }

    /// <summary>
    /// 가장 가까운 적을 타겟팅 중인지 유무를 가져오기
    /// </summary>
    /// <returns></returns>
    public bool GetTargetingBool()
    {
        return isTargeting;
    }

    /// <summary>
    /// 타겟팅 위치가 플레이어 기준 왼쪽인지 오른쪽인지 결과를 가져오기
    /// </summary>
    /// <returns></returns>
    public bool GetTargetingDirBool()
    {
        return isTargetingDir;
    }

    /// <summary>
    /// 캐릭터의 스테이터스 기본 설정
    /// </summary>
    protected virtual void SetPlayerStadardStatus()
    {
    }
}
