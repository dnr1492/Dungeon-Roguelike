using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : PlayerController
{
    //public int Hp { get { return hp; } }
    //public int Mp { get { return mp; } }

    [Header("�÷��̾� ����")]
    protected float moveSpeed;  //����
    protected int hp;
    protected int maxHp;  //����
    protected int mp;
    protected int maxMp;  //����
    
    [Header("�÷��̾� ����")]
    protected bool isDie = false;  //�׾����� true ���� �ʾ����� false

    [Header("Ÿ����")]
    private bool isTargeting = false;  //Ÿ���� ���̸� true �ƴϸ� false
    private bool isTargetingDir = false;  //Ÿ���� ��ġ�� �÷��̾� ���� �������̸� true �����̸� false
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
    /// ���̽�ƽ���� ĳ������ �̵�
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
    /// �÷��̾ �ǰ�
    /// </summary>
    public void HitPlayer()
    {
        if (goblinController.GetCurMonsterName() == MonsterController.MonsterName.Goblin)
        {
            hp -= goblinController.Damage;
            Debug.LogFormat("�÷��̾ {0}�� �������� �޾ҽ��ϴ�.", goblinController.Damage);
            Debug.LogFormat("�÷��̾��� ���� ü���� {0} �Դϴ�", hp);
        }
    }

    /// <summary>
    /// �÷��̾��� ����
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
    /// ���� ����� ���� Ÿ����
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
    /// �÷��̾ Ÿ������ ���� �ٶ󺸱�
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
    /// ���� ����� ���� Ÿ�����ϱ� ���� ������ ���
    /// </summary>
    /// <param name="target"></param>
    protected void CalRotationToTargeting(Collider2D target)
    {
        float x = target.transform.position.x - GetComponentInChildren<BoxCollider2D>().transform.position.x;
        float y = target.transform.position.y - GetComponentInChildren<BoxCollider2D>().transform.position.y;
        targetingAngle = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }

    /// <summary>
    /// ���� ����� ���� Ÿ�����ϱ� ���ؼ� ����� ������ ��������
    /// </summary>
    /// <returns></returns>
    public float GetTargetingAngle()
    {
        return targetingAngle;
    }

    /// <summary>
    /// ���� ����� ���� Ÿ���� ������ ������ ��������
    /// </summary>
    /// <returns></returns>
    public bool GetTargetingBool()
    {
        return isTargeting;
    }

    /// <summary>
    /// Ÿ���� ��ġ�� �÷��̾� ���� �������� ���������� ����� ��������
    /// </summary>
    /// <returns></returns>
    public bool GetTargetingDirBool()
    {
        return isTargetingDir;
    }

    /// <summary>
    /// ĳ������ �������ͽ� �⺻ ����
    /// </summary>
    protected virtual void SetPlayerStadardStatus()
    {
    }
}
