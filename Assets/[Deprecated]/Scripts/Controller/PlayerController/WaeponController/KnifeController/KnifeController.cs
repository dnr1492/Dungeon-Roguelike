using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnifeController : WeaponController
{
    private Coroutine coroutine;
    private BoxCollider2D boxCollider2D;
    private Vector3 originPos;

    [Header("나이프류 스탯")]
    protected float attackRange;  //스탯

    protected override void Init()
    {
        SetWeaponStadardStatus();
        SetCurWeaponType(WeaponType.Knife);
    }

    protected override void Awake()
    {
        base.Awake();

        boxCollider2D = GetComponent<BoxCollider2D>();
        originPos = transform.localPosition;
    }

    private void Update()
    {
        RotateWeapon();
        SetExtraRotationWeapon();
        RotateWeaponToTarget();

        //무기의 위치를 고정
        if (!boxCollider2D.isTrigger) transform.localPosition = originPos;
    }

    protected override void OnEnable()
    {
        SetCurWeaponType(WeaponType.Knife);
    }

    /// <summary>
    /// 무기 회전을 "20 Evolving Weapons"의 기본 이미지에 맞게 설정
    /// </summary>
    private void SetExtraRotationWeapon()
    {
        if (curSpriteRenderer != null) {
            if (characterController.GetTargetingBool()) {
                if (characterController.GetTargetingDirBool()) curSpriteRenderer.flipX = true;
                else curSpriteRenderer.flipX = false;
            }
            else {
                if (joystick.Horizontal > 0) curSpriteRenderer.flipX = true;
                else if (joystick.Horizontal < 0) {
                    curSpriteRenderer.flipX = false;
                    curSpriteRenderer.flipY = false;
                }
            }
        }
    }

    /// <summary>
    /// [칼로] 공격하는 루틴 설정
    /// </summary>
    /// <returns></returns>
    private IEnumerator SetAttackWithKnifeRoutine()
    {
        if (coroutine != null) yield break;

        boxCollider2D.isTrigger = true;
        transform.position = transform.position + (transform.up / 2) * attackRange;
        yield return new WaitForSeconds(0.05f);

        transform.position = transform.position + (transform.up / 2) * attackRange;
        yield return new WaitForSeconds(0.05f);

        boxCollider2D.isTrigger = false;
        transform.position = transform.position - (transform.up / 2) * attackRange;
        yield return new WaitForSeconds(0.05f);

        transform.position = transform.position - (transform.up / 2) * attackRange;
        coroutine = null;
    }

    /// <summary>
    /// [칼로] 공격
    /// </summary>
    public void GetAttackWithKnife()
    {
        coroutine = StartCoroutine(SetAttackWithKnifeRoutine());        
    }

    protected override void SetWeaponStadardStatus()
    {
        //attackSpeed = 0.2f;
        damage = 1;
        attackRange = 1f;
    }
}
